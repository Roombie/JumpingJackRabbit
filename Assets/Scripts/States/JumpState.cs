using UnityEngine;

public class JumpState : MovementState
{
    private bool hasStartedFalling = false;

    public JumpState(StateMachine stateMachine, JackRabbitController player) : base(stateMachine, player) { }

    public override void Enter()
    {
        Debug.Log("Enter Jump");
        player.PerformJump();
        hasStartedFalling = false;
    }

    public override void Update()
    {
        base.Update();

        Vector3 moveDir = player.GetMoveDirection();
        float speed = player.SprintInput ? player.sprintSpeed : player.moveSpeed;

        Vector3 targetVelocity = moveDir * speed;
        player.SetHorizontalVelocity(targetVelocity);
        player.RotateTowardsMoveDirection();

        if (player.JumpInput && player.jumpsRemaining > 0)
        {
            player.ConsumeJumpInput();
            stateMachine.ChangeState(new JumpState(stateMachine, player));
            return;
        }

        if (player.CurrentVelocity.y < -0.1f)
        {
            stateMachine.ChangeState(new FallState(stateMachine, player));
            return;
        }

        if (player.IsGrounded())
        {
            if (player.MoveInput.magnitude > 0.1f)
                stateMachine.ChangeState(new MoveState(stateMachine, player));
            else
                stateMachine.ChangeState(new IdleState(stateMachine, player));
            
            return;
        }
    }

    public override void Exit()
    {
        Debug.Log("Exit Jump");
    }
}