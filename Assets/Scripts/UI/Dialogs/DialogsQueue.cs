using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace UI.Dialogs
{
    public class DialogsQueue
    {
        private readonly SortedDictionary<int,Queue<Action>> queue=new SortedDictionary<int,Queue<Action>>();
        internal static DialogsQueue ShowingQueue { get; private set; }
        private Coroutine completion;
        public bool IsProcessing { get; set; }
        public void AddToQueue(int priority,Action show)
        {
            if(show==null) throw new ArgumentNullException(nameof(show));
            if(!queue.TryGetValue(priority,out var actions)) { actions=new Queue<Action>();queue.Add(priority,actions); }
            actions.Enqueue(show); if(!IsProcessing) ShowNext();
        }
        public void ShowNext()
        {
            if(completion!=null) { UICoroutineRunner.Stop(completion);completion=null; }
            if(queue.Count==0) { IsProcessing=false;return; }
            int priority=0;foreach(var key in queue.Keys) priority=key;
            var actions=queue[priority];var show=actions.Dequeue();if(actions.Count==0) queue.Remove(priority);
            var previous=ShowingQueue;ShowingQueue=this;
            IsProcessing=true;try { show(); } catch { IsProcessing=false;throw; } finally { ShowingQueue=previous; }
        }
        internal void CompleteCurrent() { if(completion!=null) return;if(Application.isPlaying) completion=UICoroutineRunner.Run(AdvanceAfterClose());else ShowNext(); }
        private IEnumerator AdvanceAfterClose() { yield return null;completion=null;ShowNext(); }
        public void Clear() { UICoroutineRunner.Stop(completion);completion=null;queue.Clear();IsProcessing=false; }
        public List<Action> GetLastList() { Queue<Action> actions=null;foreach(var value in queue.Values) actions=value;return actions==null?null:new List<Action>(actions); }
    }
}
