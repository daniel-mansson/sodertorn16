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
public struct Vec3
{
	public Vec3(int x, int y, int z)
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public int x;
	public int y;
	public int z;

	public static Vec3[] dirLookup = new Vec3[4]
	{
		new Vec3(0,0,1),
		new Vec3(1,0,0),
		new Vec3(0,0,-1),
		new Vec3(-1,0,0)
	};
	public static Vec3 VecFromDir(int dir)
	{
		return dirLookup[dir];
	}

	public static Vec3 operator +(Vec3 a, Vec3 b)
	{
		return new Vec3(a.x + b.x, a.y + b.y, a.z + b.z);
	}
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
	Rat
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
	public Vec3 pos;
	public int dir;
	public Order order;
	public ActorType type;
	public int subType;

	bool flip;

	public bool Step()
	{
		if (order != null)
		{
			//TODO
		}

		if (flip)
			pos = pos + Vec3.VecFromDir(dir);
		else
			dir = (dir + 1) % 4;

		flip = !flip;

		return true;
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
		float edgeFadeout = 1f / 32f;
		float pseed = (float)seed;
		var rand = new DeterministicRandom((uint)seed);
		float pseed2 = rand.Range(1, 10000) / 100f;
		float pseed3 = rand.Range(1, 10000) / 100f;

		for (int i = 0; i < size; i++)
		{
			for (int j = 0; j < size; j++)
			{
				float x = (float)j * 0.07f;
				float y = (float)i * 0.07f;

				float edgeFactor = 1f;
				edgeFactor = Math.Min(edgeFactor, (i * edgeFadeout));
				edgeFactor = Math.Min(edgeFactor, (j * edgeFadeout));
				edgeFactor = Math.Min(edgeFactor, ((size - i) * edgeFadeout));
				edgeFactor = Math.Min(edgeFactor, ((size - j) * edgeFadeout));

				float h = Perlin.perlin(x + pseed, y + pseed);

				h *= 16f;
				h += 6f;
				if (h > 9f)
				{
					h -= (h - 9) * 0.75f;
				}

				float h2 = Perlin.perlin(x * 1.3f + pseed2, y * 1.3f + pseed2);
				h2 -= 0.3f;
				if (h2 < 0f)
					h2 = 0f;
				h -= h2 * 5f;

				float h3 = Perlin.perlin(x * 1.3f + pseed2, y * 1.3f + pseed2);
				float dx = (j - size * 0.5f) / 20f;
				float dy = (i - size * 0.5f) / 20f;
				float l = (float)Math.Sqrt(dx * dx + dy * dy);
				if (l < 1f)
				{
					float d = 1f - l;
					h += h3 * d * 8f;
				}

				startMap[GetIdx(j, i)] = (int)(h * edgeFactor) - 8;


				map[GetIdx(j, i)] = startMap[GetIdx(j, i)];
			}
		}
	}

	public int GetIdx(int x, int y)
	{
		return y * size + x;
	}

	public int GetX(int idx)
	{
		return idx % size;
	}

	public int GetY(int idx)
	{
		return idx / size;
	}

	public int GetHeight(int x, int y)
	{
		return map[GetIdx(x, y)];
	}

	public int GetHeight(Vec3 pos)
	{
		return map[GetIdx(pos.x, pos.z)];
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
	public int idx;
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
public class GameStartState
{
	public SurvivalGameConfig config;
	public StateChange change;
}

public class Logic
{
	public List<Actor> m_rats = new List<Actor>();
	public Dictionary<int, Actor> m_lookup;
	public Terrain m_terrain;

	public Logic(Dictionary<int, Actor> lookup, Terrain terrain)
	{
		m_lookup = lookup;
		m_terrain = terrain;
	}

	public void Add(Actor actor)
	{
		if (actor.type == ActorType.Rat)
		{
			m_rats.Add(actor);
		}
	}

	public void Remove(Actor actor)
	{
		if (actor.type == ActorType.Rat)
		{
			m_rats.Remove(actor);
		}
	}

	public void Update()
	{

	}
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

		for (int i = 0; i < 8; ++i)
		{
			Vec3 pos = new Vec3();

			for (int j = 0; j < 100; ++j)
			{
				pos = new Vec3(random.Range(0, terrain.size), 0, random.Range(0, terrain.size));
				pos.y = terrain.GetHeight(pos);
				if (pos.y > 0)
					break;
			}

			actors.Add(new Actor()
			{
				id = nextId++,
				dir = random.Range(0, 4),
				subType = random.Range(0, 2),
				order = null,
				type = ActorType.Rat,
				pos = pos
			});
		}
	}

	public GameStartState GenerateStartState()
	{
		var state = new GameStartState()
		{
			config = config,
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
