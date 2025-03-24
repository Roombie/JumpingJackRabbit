public class StateMachine
{
    private State currentState;

    public void ChangeState(State newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    public void Update()
    {
        currentState?.Update();
    }

    public string GetCurrentStateName()
    {
        return currentState != null ? currentState.GetType().Name : "No State";
    }
}