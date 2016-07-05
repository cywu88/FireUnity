using UnityEngine;
using System.Collections;

/// <summary>
/// 状态机
/// </summary>
/// <typeparam name="entity_type"></typeparam>
public class StateMachine<entity_type>
{
    //a pointer to the agent that owns this instance
    entity_type          m_pOwner;
 
    State<entity_type>   m_pCurrentState;
 
    //a record of the last state the agent was in
    State<entity_type>   m_pPreviousState;
 
    //this state logic is called every time the FSM is updated
    State<entity_type>   m_pGlobalState;
 
    public StateMachine(entity_type owner)
    {
        m_pOwner = owner;
        m_pCurrentState = null;
        m_pPreviousState = null;
        m_pGlobalState = null;
    }
 
    /// <summary>
    /// 更新
    /// </summary>
    public void Update()
    {
        //if a global state exists, call its execute method
        if (m_pGlobalState != null)   m_pGlobalState.Execute(m_pOwner);
 
        //same for the current state
        if (m_pCurrentState != null) m_pCurrentState.Execute(m_pOwner);
    }

    public bool HandleMessage(Telegram msg)
    {
        //first see if the current state is valid and that it can handle
        //the message
        if (m_pCurrentState != null && m_pCurrentState.OnMessage(m_pOwner, msg))
        {
            return true;
        }
  
        //if not, and if a global state has been implemented, send 
        //the message to the global state
        if (m_pGlobalState != null && m_pGlobalState.OnMessage(m_pOwner, msg))
        {
            return true;
        }

        return false;
    }
 
    /// <summary>
    /// 切换状态
    /// </summary>
    /// <param name="pNewState"></param>
    public void  ChangeState(State<entity_type> pNewState)
    {
        //keep a record of the previous state
        m_pPreviousState = m_pCurrentState;
 
        //call the exit method of the existing state
        m_pCurrentState.Exit(m_pOwner);
 
        //change state to the new state
        m_pCurrentState = pNewState;
 
        //call the entry method of the new state
        m_pCurrentState.Enter(m_pOwner);
    }
 
    /// <summary>
    /// 返回上一个状态
    /// </summary>
    public void  RevertToPreviousState()
    {
        ChangeState(m_pPreviousState);
    }

    //use these methods to initialize the FSM
    public void SetCurrentState(State<entity_type> s) { m_pCurrentState = s; }
    public void SetGlobalState(State<entity_type> s) { m_pGlobalState = s; }
    public void SetPreviousState(State<entity_type> s) { m_pPreviousState = s; }

    //accessors
    public State<entity_type>  CurrentState()  {return m_pCurrentState;}
    public State<entity_type>  GlobalState()   {return m_pGlobalState;}
    public State<entity_type>  PreviousState() {return m_pPreviousState;}
};