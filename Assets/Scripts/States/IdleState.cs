using UnityEngine;

public class IdleState : MovementState
{
    public IdleState(StateMachine stateMachine, JackRabbitController player) : base(stateMachine, player) { }

    public override void Enter()
    {
        Debug.Log("Enter Idle");
    }

    public override void Update()
    {
        base.Update();

        if (player.HasBufferedJump() && (player.IsGrounded() || player.IsInCoyoteTime()))
        {
            player.ConsumeBufferedJump();
            stateMachine.ChangeState(new JumpState(stateMachine, player));
            return;
        }

        if (player.MoveInput.magnitude > 0.1f)
        {
            stateMachine.ChangeState(new MoveState(stateMachine, player));
            return;
        }

        if (!player.IsGrounded())
        {
            stateMachine.ChangeState(new FallState(stateMachine, player));
            return;
        }
    }

    public override void Exit()
    {
        Debug.Log("Exit Idle");
    }
}