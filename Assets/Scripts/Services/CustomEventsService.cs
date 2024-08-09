using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using GameplayServices.Common;

public class CustomEventsService : MonoBehaviour
{
    //for the test, you can use https://posttestserver.dev
    [SerializeField] private string serverUrl;
    [SerializeField] private float cooldownBeforeSendSecond = 2f;
    [SerializeField] private int maxSendingBytes = 1024;
    [SerializeField] private int sendTimeoutSecond = 1;
    
    private IWebRequest _sender;
    private TimeSpan _cooldownTimer;
    private UniTask _sendingTask;
    private FileHelper _files;

    private void Start()
    {
        Initialize();
        _sendingTask = StartEventsSending();
    }

    public void Initialize(IWebRequest sender = null)
    {
        this._sender = sender ?? new WebRequest(sendTimeoutSecond);
        _cooldownTimer = TimeSpan.FromSeconds(cooldownBeforeSendSecond);
        _files = new FileHelper("events");
    }

    public void TrackEvent(string type, string data)
    {
        _files.SaveToFile(new Event { Type = type, Data = data });
        
        if(CanSending) 
            _sendingTask = StartEventsSending();
    }

    private async UniTask StartEventsSending()
    {
        while (!_files.IsEmpty)
        {
            await UniTask.WhenAll(RunCooldownTimerAsync(), SendEventsAsync());
        }
    }
    private bool CanSending => _sendingTask.Status.IsCompleted();
    private async UniTask RunCooldownTimerAsync() => await UniTask.Delay(_cooldownTimer);
    private async UniTask SendEventsAsync()
    {
        var events = _files.LoadFromFiles<Event>(maxSendingBytes);
        var eventsJson = JsonUtility.ToJson(new EventWrapper { Events = events });
        var isSuccessful = await _sender.PostAsync(serverUrl, eventsJson);
        if(isSuccessful)
        {
            _files.DeleteLoadedFiles();
        }
        
        Debug.Log($"Send:{isSuccessful} Body:{eventsJson}");

        await UniTask.Yield();
    }

    [System.Serializable]
    public class Event
    {
        public string Type;
        public string Data;
    }

    [System.Serializable]
    private class EventWrapper
    {
        public List<Event> Events;
    }

    #if UNITY_EDITOR
    [ContextMenu("SEND")]
    public async UniTask SendDEBUG()
    {
        TrackEvent("1", "1");
        await UniTask.Delay(TimeSpan.FromSeconds(2));
        TrackEvent("2", "2");
        TrackEvent("3", "3");
        TrackEvent("4", "4");
        TrackEvent("5", "5");
        await UniTask.Delay(TimeSpan.FromSeconds(3));
        TrackEvent("6", "6");
        TrackEvent("7", "7");
        TrackEvent("8", "8");
        TrackEvent("9", "9");
        await UniTask.Delay(TimeSpan.FromSeconds(1));
        TrackEvent("10", "10");
        TrackEvent("11", "11");
        TrackEvent("12", "12");
        TrackEvent("13", "13");
    }
    #endif
}