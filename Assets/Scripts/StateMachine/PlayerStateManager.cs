using UnityEngine;

public class PlayerStateManager : MonoBehaviour
{ 
    PlayerBaseState currentState;
    //if a new state gets added add it below here
    PlayerNotepadState NotepadState = new PlayerNotepadState();
    PlayerMoveState MoveState = new PlayerMoveState();
    PlayerKeypadState KeypadState = new PlayerKeypadState();
    PlayerHackingState HackingState = new PlayerHackingState();
    PlayerIdleState IdleState = new PlayerIdleState();
    /* when making a new state copy this into the script
        public override void EnterState (PlayerStateManager player)
    {

    }
   public override void UpdateState(PlayerStateManager player)
    {

    }
   public override void OnCollisionEnter(PlayerStateManager player)
    {

    } */
    private void Start()
    {
        currentState = IdleState;
    }
}
