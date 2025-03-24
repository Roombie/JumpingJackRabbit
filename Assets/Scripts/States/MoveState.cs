using UnityEngine;

public class MoveState : MovementState
{
    public MoveState(StateMachine stateMachine, JackRabbitController player) : base(stateMachine, player) { }

    public override void Enter()
    {
        Debug.Log("Enter Move");
    }

    public override void Update()
    {
        base.Update();

        Vector3 moveDir = player.GetMoveDirection();
        float speed = player.SprintInput ? player.sprintSpeed : player.moveSpeed;

        Vector3 targetVelocity = moveDir * speed;
        player.SetHorizontalVelocity(targetVelocity);
        player.RotateTowardsMoveDirection();

        if (player.HasBufferedJump() && (player.IsGrounded() || player.IsInCoyoteTime()))
        {
            player.ConsumeBufferedJump();
            stateMachine.ChangeState(new JumpState(stateMachine, player));
            return;
        }

        if (!player.IsGrounded())
        {
            stateMachine.ChangeState(new FallState(stateMachine, player));
            return;
        }

        if (player.MoveInput.magnitude < 0.1f)
        {
            stateMachine.ChangeState(new IdleState(stateMachine, player));
            return;
        }
    }

    public override void Exit()
    {
        Debug.Log("Exit Move");
    }
}