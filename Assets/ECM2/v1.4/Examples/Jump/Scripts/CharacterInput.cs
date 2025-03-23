﻿using UnityEngine;

namespace ECM2.Examples.Jump
{
    /// <summary>
    /// Handle character's input and Jump ability input.
    /// </summary>
    
    public class CharacterInput : MonoBehaviour
    {
        // The controlled Character
        
        private Character _character;
        
        // The jump ability
        
        private JumpAbility _jumpAbility;

        private void Awake()
        {
            // Cache components
            
            _character = GetComponent<Character>();
            _jumpAbility = GetComponent<JumpAbility>();
        }

        private void Update()
        {
            // Movement input
            
            Vector2 inputMove = new Vector2()
            {
                x = Input.GetAxisRaw("Horizontal"),
                y = Input.GetAxisRaw("Vertical")
            };
            
            Vector3 movementDirection =  Vector3.zero;

            movementDirection += Vector3.right * inputMove.x;
            movementDirection += Vector3.forward * inputMove.y;
            
            // If character has a camera assigned...
            
            if (_character.camera)
            {
                // Make movement direction relative to its camera view direction
                
                movementDirection = movementDirection.relativeTo(_character.cameraTransform);
            }

            _character.SetMovementDirection(movementDirection);
            
            // Crouch input
            
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
                _character.Crouch();
            else if (Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.C))
                _character.UnCrouch();
            
            // Jump input
            
            if (Input.GetButtonDown("Jump"))
                _jumpAbility.Jump();
            else if (Input.GetButtonUp("Jump"))
                _jumpAbility.StopJumping();
        }
    }
}