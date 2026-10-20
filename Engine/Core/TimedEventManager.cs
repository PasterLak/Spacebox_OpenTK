using Engine;

public sealed class TimedEventManager
{
    private List<TimedEvent> events;

    public TimedEventManager()
    {
        events = new List<TimedEvent>();
    }

    public void ExecuteOnce(int timeDelay, Action eventFunction)
    {
        ExecuteMultipleTimes(timeDelay, 1, eventFunction);
    }

    public void ExecuteMultipleTimes(int timeDelay, int executionCount, Action eventFunction)
    {
        if (eventFunction != null)
        {
            TimedEvent item = new TimedEvent
            {
                EventFunction = eventFunction,
                ExecutionCount = executionCount,
                ExecutionInterval = timeDelay,
                TimeUntilExecution = timeDelay
            };
            events.Add(item);
        }
    }

    public void OnUpdate()
    {
        for (int i = events.Count - 1; i >= 0; i--)
        {
            TimedEvent timedEvent = events[i];

            timedEvent.TimeUntilExecution -= Time.Delta;

            if (timedEvent.TimeUntilExecution <= 0)
            {
                try
                {
                    timedEvent.EventFunction();
                }
                catch (Exception ex)
                {
                    //Debug.LogError("Exeption thrown in timed event function: " + ex.Message);
                    events.RemoveAt(i);
                }

                timedEvent.ExecutionCount--;
                timedEvent.TimeUntilExecution = timedEvent.ExecutionInterval;

                if (timedEvent.ExecutionCount <= 0)
                {
                    this.events.RemoveAt(i);
                }
            }
        }
    }

    private class TimedEvent
    {
        public Action EventFunction;

        public float TimeUntilExecution;

        public int ExecutionInterval;

        public int ExecutionCount;
    }
}