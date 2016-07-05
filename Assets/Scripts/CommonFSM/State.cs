using UnityEngine;
using System.Collections;

/// <summary>
/// ×´Ì¬»ùÀà
/// </summary>
/// <typeparam name="entity_type"></typeparam>
public class State<entity_type>
{
    //this will execute when the state is entered
    internal protected virtual void Enter(entity_type player) { }
    
    //this is the states normal update function
    internal protected virtual void Execute(entity_type player) { }
  
    //this will execute when the state is exited. 
    internal protected virtual void Exit(entity_type player) { }
    
    //this executes if the agent receives a message from the 
    //message dispatcher
    internal protected virtual bool OnMessage(entity_type player, Telegram msg) { return false; }

}