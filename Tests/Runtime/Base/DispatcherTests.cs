using NUnit.Framework;
using ReactUnity.Tests.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace ReactUnity.Tests
{
    [TestFixture]
    public class DispatcherTests
    {

        [UnityTest, ReactInjectableTest]
        public IEnumerator ReactComponent_AfterDestroyed_DeferredsShouldStopRunning()
        {
            yield return null;

            var canvas = GameObject.Find("REACT_CANVAS").GetComponent<ReactUnity>();

            var ctx = canvas.Context;

            var view = ctx.Host.Children[0];

            ReactUnityAPI.Instance.removeChild(ctx.Host, view);

            var tmp = canvas.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            Assert.Null(tmp);
        }


        [UnityTest]
        public IEnumerator RuntimeDispatcher_OnEveryUpdate_RunsOnEachUpdate()
        {
            var dispatcher = RuntimeDispatcher.Create();

            var value = 0;

            var handle = dispatcher.OnEveryUpdate(() => value++);

            yield return null;
            yield return null;
            Assert.AreEqual(1, value);

            yield return null;
            Assert.AreEqual(2, value);

            dispatcher.StopDeferred(handle);

            yield return null;
            yield return null;
            yield return null;
            Assert.AreEqual(2, value, "Deferred failed to stop");
        }

        [Ignore("LateUpdate Coroutine is not ready yet")]
        [UnityTest]
        public IEnumerator RuntimeDispatcher_OnEveryLateUpdate_RunsOnEachLateUpdate()
        {
            var dispatcher = RuntimeDispatcher.Create();

            var value = 0;

            var handle = dispatcher.OnEveryLateUpdate(() => value++);

            yield return null;
            Assert.AreEqual(1, value);

            yield return null;
            Assert.AreEqual(2, value);

            dispatcher.StopDeferred(handle);

            yield return null;
            yield return null;
            yield return null;
            Assert.AreEqual(2, value, "Deferred failed to stop");
        }



        [UnityTest]
        public IEnumerator RuntimeDispatcher_OnEveryUpdate_StopDoesNotFailUnderLoad()
        {
            var dispatcher = RuntimeDispatcher.Create();

            var value = 0;

            var othersToStop = new List<int>();


            void stopRandom()
            {
                for (int i = 0; i < 100; i++)
                    dispatcher.StopDeferred(othersToStop[Mathf.FloorToInt(Random.value * (othersToStop.Count - 2))]);
            }

            for (int i = 0; i < 2400; i++)
                othersToStop.Add(dispatcher.OnEveryUpdate(() => { }));

            stopRandom();

            var handle = dispatcher.OnEveryUpdate(() => value++);

            for (int i = 0; i < 3000; i++)
                othersToStop.Add(dispatcher.OnEveryUpdate(() => { }));

            yield return null;
            stopRandom();

            yield return null;
            Assert.AreEqual(1, value);

            stopRandom();

            yield return null;
            Assert.AreEqual(2, value);

            stopRandom();
            dispatcher.StopDeferred(handle);
            stopRandom();

            yield return null;
            stopRandom();

            yield return null;
            stopRandom();

            yield return null;
            Assert.AreEqual(2, value, "Deferred failed to stop");
        }
    }
}