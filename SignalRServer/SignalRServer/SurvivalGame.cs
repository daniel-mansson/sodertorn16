using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class DeterministicRandom
{
	public uint m_w;
	public uint m_z;

	public DeterministicRandom(uint seed)
	{
		m_w = seed;
		m_z = seed;

		GetValue();
	}

	public int Range(int min, int max)
	{
		uint d = (uint)(max - min);
		return (int)(GetValue() % d) - min;
	}

	uint GetValue()
	{
		m_z = 36969 * (m_z & 65535) + (m_z >> 16);
		m_w = 18000 * (m_w & 65535) + (m_w >> 16);
		return (uint)((m_z << 16) + m_w);
	}
}

[Serializable]
public struct Vec2
{
	public Vec2(int x, int y)
	{
		this.x = x;
		this.y = y;
	}

	public int x;
	public int y;
}

public enum ActorActionType
{
	Move,
	Attack,
	Grab,
	Drop
}

public enum ActorType
{
	Player,
	SmallMonster
}

[Serializable]
public class ActorAction
{
	public int target;
	public int startFrame;
	public int endFrame;
	public int actionFrame;
	public ActorActionType type;
}

[Serializable]
public class Order
{
	public List<ActorAction> actions;
}

[Serializable]
public class Actor
{
	public int id;
	public Vec2 pos;
	public int dir;
	public Order order;
	public ActorType type;
	public int subType;

	public bool Step()
	{
		if (order != null)
		{
			//TODO
		}

		return false;
	}
}

[Serializable]
public class Terrain
{
	public int size;
	public int[] startMap;
	public int[] map;
	public List<TerrainChange> changeSinceStart;

	public Terrain(SurvivalGameConfig config)
	{
		changeSinceStart = new List<TerrainChange>();

		size = config.mapSize;
		map = new int[size * size];
		startMap = new int[size * size];
	}

	public void Generate(int seed)
	{
		//TODO:
	}
}

[Serializable]
public class SurvivalGameConfig
{
	public int seed;
	public int mapSize;
}

[Serializable]
public class TerrainChange
{
	public Vec2 pos;
	public int height;
}

[Serializable]
public class StateChange
{
	public int frame;
	public List<Actor> actors;
	public List<Actor> killedActors;
	public List<TerrainChange> terrainChange;
}

[Serializable]
public class StartState
{
	public int seed;
	public StateChange change;
}

[Serializable]
public class SurvivalGame
{
	public DeterministicRandom random;
	public List<Actor> actors;
	public Terrain terrain;
	public SurvivalGameConfig config;
	public int frame;
	public int nextId;

	public SurvivalGame(SurvivalGameConfig config)
	{
		this.config = config;
		terrain = new Terrain(config);
		actors = new List<Actor>();
		frame = 0;
		random = new DeterministicRandom((uint)config.seed);

		GenerateWorld();
	}

	void GenerateWorld()
	{
		terrain.Generate(config.seed);

		for (int i = 0; i < 4; ++i)
		{
			actors.Add(new Actor()
			{
				id = nextId,
				dir = random.Range(0, 4),
				subType = random.Range(0, 2),
				order = null,
				type = ActorType.SmallMonster,
				pos = new Vec2(random.Range(0, 20), random.Range(0, 20))
			});
		}
	}

	public StartState GenerateStartState()
	{
		var state = new StartState()
		{
			seed = config.seed,
			change = new StateChange()
			{
				actors = actors,
				frame = frame,
				killedActors = new List<Actor>(),
				terrainChange = terrain.changeSinceStart
			}
		};

		return state;
	}

	public StateChange Step()
	{
		++frame;
		var change = new StateChange()
		{
			actors = new List<Actor>(),
			frame = frame,
			killedActors = new List<Actor>(),
			terrainChange = new List<TerrainChange>()
		};

		foreach (var actor in actors)
		{
			bool anyChange = actor.Step();

			if (anyChange)
			{
				change.actors.Add(actor);
			}
		}

		return change;
	}
}
