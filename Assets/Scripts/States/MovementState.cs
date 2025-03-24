public abstract class MovementState : State
{
    protected JackRabbitController player;

    public MovementState(StateMachine stateMachine, JackRabbitController player) : base(stateMachine)
    {
        this.player = player;
    }

    public override void Update()
    {
        base.Update();
        player.ApplyGravity();
    }
}