using System.Collections;
using System.Collections.Generic;
using Godot;


/// <summary>
/// basic CoroutineManager. Coroutines can do the following:
/// - yield return null (tick again the next frame)
/// - yield return Coroutine.waitForSeconds( 3 ) (tick again after a 3 second delay)
/// - yield return Coroutine.waitForSeconds( 5.5f ) (tick again after a 5.5 second delay)
/// - yield return startCoroutine( another() ) (wait for the other coroutine before getting ticked again)
/// </summary>
public class CoroutineManager : Node
{
    /// <summary>
    /// internal class used by the CoroutineManager to hide the data it requires for a Coroutine
    /// </summary>
    private class CoroutineImpl : ICoroutine, IPoolable
    {
        public IEnumerator Enumerator;

        /// <summary>
        /// anytime a delay is yielded it is added to the waitTimer which tracks the delays
        /// </summary>
        public float WaitTimer;

        public bool IsDone;
        public CoroutineImpl WaitForCoroutine;
        //public bool UseUnscaledDeltaTime = false;


        public void Stop()
        {
            IsDone = true;
        }


        public ICoroutine SetUseUnscaledDeltaTime(bool useUnscaledDeltaTime)
        {
            //UseUnscaledDeltaTime = useUnscaledDeltaTime;
            return this;
        }


        internal void PrepareForReuse()
        {
            IsDone = false;
        }


        void IPoolable.Reset()
        {
            IsDone = true;
            WaitTimer = 0;
            WaitForCoroutine = null;
            Enumerator = null;
            //UseUnscaledDeltaTime = false;
        }
    }

    //The instance of this manager
    private static CoroutineManager _instance;

    /// <summary>
    /// flag to keep track of when we are in our update loop. If a new coroutine is started during the update loop we have to stick
    /// it in the shouldRunNextFrame List to avoid modifying a List while we iterate.
    /// </summary>
    private bool _isInUpdate;
    private List<CoroutineImpl> _unblockedCoroutines = new List<CoroutineImpl>();
    private List<CoroutineImpl> _shouldRunNextFrame = new List<CoroutineImpl>();


    /// <summary>
    /// adds the IEnumerator to the CoroutineManager. Coroutines get ticked before Update is called each frame.
    /// </summary>
    /// <returns>The coroutine.</returns>
    /// <param name="enumerator">Enumerator.</param>
    protected ICoroutine Start(IEnumerator enumerator)
    {
        // find or create a CoroutineImpl
        var coroutine = Pool<CoroutineImpl>.Obtain();
        coroutine.PrepareForReuse();

        // setup the coroutine and add it
        coroutine.Enumerator = enumerator;
        var shouldContinueCoroutine = TickCoroutine(coroutine);

        // guard against empty coroutines
        if (!shouldContinueCoroutine)
        {
            return null;
        }

        if (_isInUpdate)
        {
            _shouldRunNextFrame.Add(coroutine);
        }
        else
        {
            _unblockedCoroutines.Add(coroutine);
        }

        return coroutine;
    }

    /// <summary>
    /// starts a coroutine. Coroutines can yeild ints/floats to delay for seconds or yeild to other calls to startCoroutine.
    /// Yielding null will make the coroutine get ticked the next frame.
    /// </summary>
    /// <returns>The coroutine.</returns>
    /// <param name="enumerator">Enumerator.</param>
    public static ICoroutine StartCoroutine(IEnumerator enumerator)
    {
        return _instance.Start(enumerator);
    }

    public override void _Ready()
    {
        _instance = this;
    }

    public override void _Process(float delta)
    {
        _isInUpdate = true;
        for (var i = 0; i < _unblockedCoroutines.Count; i++)
        {
            var coroutine = _unblockedCoroutines[i];

            // check for stopped coroutines
            if (coroutine.IsDone)
            {
                Pool<CoroutineImpl>.Free(coroutine);
                continue;
            }

            // are we waiting for any other coroutines to finish?
            if (coroutine.WaitForCoroutine != null)
            {
                if (coroutine.WaitForCoroutine.IsDone)
                {
                    coroutine.WaitForCoroutine = null;
                }
                else
                {
                    _shouldRunNextFrame.Add(coroutine);
                    continue;
                }
            }

            // deal with timers if we have them
            if (coroutine.WaitTimer > 0)
            {
                // still has time left. decrement and run again next frame being sure to decrement with the appropriate deltaTime.
                //coroutine.WaitTimer -= coroutine.UseUnscaledDeltaTime ? Time.UnscaledDeltaTime : Time.DeltaTime;
                coroutine.WaitTimer -= delta;

                _shouldRunNextFrame.Add(coroutine);
                continue;
            }

            if (TickCoroutine(coroutine))
            {
                _shouldRunNextFrame.Add(coroutine);
            }
        }

        _unblockedCoroutines.Clear();
        _unblockedCoroutines.AddRange(_shouldRunNextFrame);
        _shouldRunNextFrame.Clear();

        _isInUpdate = false;
    }

    /// <summary>
    /// ticks a coroutine. returns true if the coroutine should continue to run next frame. This method will put finished coroutines
    /// back in the Pool!
    /// </summary>
    /// <returns><c>true</c>, if coroutine was ticked, <c>false</c> otherwise.</returns>
    /// <param name="coroutine">Coroutine.</param>
    private bool TickCoroutine(CoroutineImpl coroutine)
    {
        // This coroutine has finished
        if (!coroutine.Enumerator.MoveNext() || coroutine.IsDone)
        {
            Pool<CoroutineImpl>.Free(coroutine);
            return false;
        }

        if (coroutine.Enumerator.Current == null)
        {
            // yielded null. run again next frame
            return true;
        }

        if (coroutine.Enumerator.Current is WaitForSecondsHelper)
        {
            coroutine.WaitTimer = (coroutine.Enumerator.Current as WaitForSecondsHelper).waitTime;
            return true;
        }

#if DEBUG

        // deprecation warning for yielding an int/float
        if (coroutine.Enumerator.Current is int)
        {
            coroutine.WaitTimer = (int)coroutine.Enumerator.Current;
            return true;
        }

        if (coroutine.Enumerator.Current is float)
        {
            coroutine.WaitTimer = (float)coroutine.Enumerator.Current;
            return true;
        }
#endif

        if (coroutine.Enumerator.Current is CoroutineImpl)
        {
            coroutine.WaitForCoroutine = coroutine.Enumerator.Current as CoroutineImpl;
            return true;
        }
        else
        {
            // This coroutine yielded some value we don't understand. run it next frame.
            return true;
        }
    }
}
