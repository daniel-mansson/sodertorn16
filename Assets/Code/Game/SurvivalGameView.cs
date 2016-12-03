using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SurvivalGameView : MonoBehaviour
{
	public List<ActorView> m_actorViewPrefabs;
	public TerrainView m_terrainView;

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

	void KillActor(Actor actor)
	{
		if (m_actors.ContainsKey(actor.id))
		{
			m_actors[actor.id].Kill();
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
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
