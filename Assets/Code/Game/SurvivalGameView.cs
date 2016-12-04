using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class SurvivalGameView : MonoBehaviour
{
	public List<ActorView> m_actorViewPrefabs;
	public TerrainView m_terrainView;
	public CameraRig m_cameraRig;
	int m_localPlayerId = -1;
	ActorView m_localPlayerView;

	Dictionary<ActorType, ActorView> m_actorViewsPrefabDict = new Dictionary<ActorType, ActorView>();
	Dictionary<int, ActorView> m_actors = new Dictionary<int, ActorView>();

	void Awake()
	{
		foreach (var a in m_actorViewPrefabs)
		{
			m_actorViewsPrefabDict.Add(a.m_type, a);
		}
	}

	public void Init(GameStartState startState)
	{
		m_terrainView.Init(startState);

		foreach (var av in m_actors)
		{
			Destroy(av.Value.gameObject);
		}
		m_actors.Clear();

		foreach (var a in startState.change.actors)
		{
			HandleActor(a);
		}
	}

	public void UpdateState(StateChange change)
	{
		foreach (var a in change.actors)
		{
			HandleActor(a);
		}

		foreach (var a in change.killedActors)
		{
			KillActor(a);
		}
	}

	public void SetLocalPlayerId(int localPlayerId)
	{
		m_localPlayerId = localPlayerId;

		if (m_actors.ContainsKey(m_localPlayerId))
		{
			var av = m_actors[m_localPlayerId];
			m_localPlayerView = av;
			m_cameraRig.SetAndTele(av.transform);
		}
	}

	void KillActor(Actor actor)
	{
		if (m_actors.ContainsKey(actor.id))
		{
			m_actors[actor.id].Kill();

			if (actor.id == m_localPlayerId)
			{
				m_localPlayerId = -1;
				m_localPlayerView = null;
			}
		}
		else
		{
			Debug.Log("KILL! Does not have actor " + actor.type + " " + actor.id);
		}
	}

	void HandleActor(Actor actor)
	{
		if (m_actors.ContainsKey(actor.id))
		{
			m_actors[actor.id].UpdateActor();
		}
		else
		{
			SpawnActor(actor);
		}
	}

	void SpawnActor(Actor actor)
	{
		var av = (ActorView)Instantiate(m_actorViewsPrefabDict[actor.type], new Vector3(actor.pos.x, actor.pos.y, actor.pos.z), Quaternion.identity);
		av.transform.parent = transform;
		av.Init(actor);
		m_actors.Add(actor.id, av);

		if (actor.id == m_localPlayerId)
		{
			m_localPlayerView = av;
			m_cameraRig.SetAndTele(av.transform);
		}
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
