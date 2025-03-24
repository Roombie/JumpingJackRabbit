using UnityEngine;

public class FallState : MovementState
{
    public FallState(StateMachine stateMachine, JackRabbitController player) : base(stateMachine, player) { }

    public override void Enter()
    {
        Debug.Log("Enter Fall");
    }

    public override void Update()
    {
        base.Update();

        Vector3 moveDir = player.GetMoveDirection();
        float speed = player.SprintInput ? player.sprintSpeed : player.moveSpeed;

        Vector3 targetVelocity = moveDir * speed;
        player.SetHorizontalVelocity(targetVelocity);
        player.RotateTowardsMoveDirection();

        if (player.HasBufferedJump() && player.jumpsRemaining > 0)
        {
            player.ConsumeBufferedJump();
            stateMachine.ChangeState(new JumpState(stateMachine, player));
            return;
        }

        if (player.IsGrounded())
        {
            if (moveDir.magnitude > 0.1f)
                stateMachine.ChangeState(new MoveState(stateMachine, player));
            else
                stateMachine.ChangeState(new IdleState(stateMachine, player));

            return;
        }
    }

    public override void Exit()
    {
        Debug.Log("Exit Fall");
    }
}