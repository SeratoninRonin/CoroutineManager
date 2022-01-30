# CoroutineManager

Unity-style coroutines in Godot C#

The CoroutineManager lets you pass in an IEnumerator which is then ticked each frame allowing you to break long running tasks up into smaller bits. The entry point for starting a coroutine is CoroutineManager.StartCoroutine which returns an ICoroutine object with a single method: Stop. The execution of a coroutine can be paused at any point using the yield statement. 

Coroutines can yield a few different ways:
- yield return null (tick again the next frame)
- yield return Coroutine.WaitForSeconds( 3 ) (tick again after a 3 second delay)
- yield return CoroutineManager.StartCoroutine( another() ) (wait for the other coroutine before getting ticked again)

# Usage

The CoroutineManager.tscn file should be added to your Project Settings -> AutoLoad as a global, like so:

![CMload](https://user-images.githubusercontent.com/61599196/151710358-1330b879-31dc-4814-a1c7-6350bc1568b9.png)

# Example
```C#
using System.Collections;

public IEnumerator MyCoroutine()
{
    if(waitCondition)
    {
      yield return Coroutine.WaitForSeconds(1);
    }
    
    for(int i=0;i<10000;i++)
    {
      if(search[i]==result)
        break;
      else
        yield return null;
    }
}

public override void __Ready()
{
    var coroutine = CoroutineManager.StartCoroutine(MyCoroutine());
    if(stopCondition)
    {
      coroutine.Stop();
    }
}
```

# License

This project is MIT licensed and provided as-is.
