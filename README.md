# CoroutineManager

Unity-style coroutines in Godot C#

The CoroutineManager lets you pass in an IEnumerator which is then ticked each frame allowing you to break long running tasks up into smaller bits. The entry point for starting a coroutine is Core.StartCoroutine which returns an ICoroutine object with a single method: Stop. The execution of a coroutine can be paused at any point using the yield statement. You can yield a call to Coroutine.WaitForSeconds which will delay execution for N seconds or you can yield a call to StartCoroutine to pause until another coroutine completes.
