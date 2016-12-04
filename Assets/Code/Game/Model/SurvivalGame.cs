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
	public static Vec3 FromDir(int dir)
	{
		return dirLookup[dir];
	}

	public static Vec3 operator +(Vec3 a, Vec3 b)
	{
		return new Vec3(a.x + b.x, a.y + b.y, a.z + b.z);
	}

	public static bool operator ==(Vec3 a, Vec3 b)
	{
		return a.x == b.x && a.y == b.y && a.z == b.z;
	}

	public static bool operator !=(Vec3 a, Vec3 b)
	{
		return !(a.x == b.x && a.y == b.y && a.z == b.z);
	}
}

public enum ActorActionType
{
	Move,
	Attack,
	Grab,
	Drop,
	Rotate
}

public enum ActorType
{
	Player,
	Rat
}

[Serializable]
public class ActorAction
{
	public Vec3 target;
	public int startDelay;
	public int endDelay;
	public ActorActionType type;
	public bool hasBeenPerformed;
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
	public string name = "";

	public bool Step(Dictionary<int, Actor> lookup, Terrain terrain)
	{
		bool anyChange = false;

		if (order != null && order.actions != null)
		{
			var first = order.actions.FirstOrDefault();

			if (first != null)
			{
				if (first.startDelay > 0)
				{
					first.startDelay--;
				}
				else
				{
					if (!first.hasBeenPerformed)
					{
						if (first.type == ActorActionType.Move)
						{
							bool clear = !lookup.ContainsKey(terrain.GetIdx(first.target));
							if (clear)
								pos = first.target;
							else
								order.actions.Clear();//Interrupt! Pow!

						}
						else if (first.type == ActorActionType.Rotate)
						{
							dir = (dir + first.target.x) % 4;
						}

						first.hasBeenPerformed = true;
						anyChange = true;
					}

					if (first.endDelay > 0)
					{
						first.endDelay--;
					}
					else
					{
						if (order.actions.Count > 0)
							order.actions.RemoveAt(0);
					}
				}
			}

			if (order.actions.Count == 0)
			{
				order = null;
			}
		}

		return anyChange;
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

	public int GetIdx(Vec3 pos)
	{
		return GetIdx(pos.x, pos.z);
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
	List<Actor> m_rats = new List<Actor>();
	Dictionary<int, Actor> m_lookup;
	Terrain m_terrain;
	DeterministicRandom m_random;

	public Logic(Dictionary<int, Actor> lookup, Terrain terrain, int seed)
	{
		m_lookup = lookup;
		m_terrain = terrain;
		m_random = new DeterministicRandom((uint)seed);
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
		foreach (var rat in m_rats)
		{
			if (rat.order == null || rat.order.actions == null)
			{
				if (m_random.Range(0, 100) > 20)
				{
					Vec3 npos = rat.pos + Vec3.FromDir(rat.dir);
					int nh = m_terrain.GetHeight(npos);
					int dh = nh - rat.pos.y;
					npos.y = nh;

					if (dh <= 1 && nh > 0 && m_random.Range(0, 100) > 30)
					{
						rat.order = new Order()
						{
							actions = new List<ActorAction>()
							{
								new ActorAction()
								{
									target = npos,
									startDelay = 0,
									endDelay = 0,
									type = ActorActionType.Move
								}
							}
						};
					}
					else
					{
						rat.order = new Order()
						{
							actions = new List<ActorAction>()
							{
								new ActorAction()
								{
									target = new Vec3(m_random.Range(0, 2) == 0 ? 1 : 3, 0, 0),
									startDelay = 0,
									endDelay = 0,
									type = ActorActionType.Rotate
								}
							}
						};
					}
				}
			}
		}
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

	Dictionary<int, Actor> m_lookup = new Dictionary<int, Actor>();
	Logic m_logic;
	List<Actor> m_newPlayers = new List<Actor>();

	public SurvivalGame(SurvivalGameConfig config)
	{
		this.config = config;
		terrain = new Terrain(config);
		actors = new List<Actor>();
		frame = 0;
		random = new DeterministicRandom((uint)config.seed);

		m_logic = new Logic(m_lookup, terrain, config.seed);

		GenerateWorld();
	}

	public int JoinGame(string name)
	{
		Vec3 pos = GetFreeSpot();

		var a = new Actor()
		{
			id = nextId++,
			dir = random.Range(0, 4),
			subType = random.Range(0, 100000),
			order = null,
			type = ActorType.Player,
			pos = pos,
			name = name
		};

		SpawnActor(a);
		m_newPlayers.Add(a);
		return a.id;
	}

	Vec3 GetFreeSpot()
	{
		Vec3 pos = new Vec3();
		for (int j = 0; j < 100; ++j)
		{
			pos = new Vec3(random.Range(0, terrain.size), 0, random.Range(0, terrain.size));

			if (m_lookup.ContainsKey(terrain.GetIdx(pos)))
				continue;

			pos.y = terrain.GetHeight(pos);

			if (pos.y > 0)
				break;
		}
		return pos;
	}

	void SpawnActor(Actor actor)
	{
		actors.Add(actor);
		m_logic.Add(actor);
		m_lookup.Add(terrain.GetIdx(actor.pos), actor);
	}

	void KillActor(Actor actor)
	{
		actors.Remove(actor);
		m_logic.Remove(actor);
		m_lookup.Remove(terrain.GetIdx(actor.pos));
	}

	void GenerateWorld()
	{
		terrain.Generate(config.seed);

		for (int i = 0; i < 8; ++i)
		{
			Vec3 pos = GetFreeSpot();

			var a = new Actor()
			{
				id = nextId++,
				dir = random.Range(0, 4),
				subType = random.Range(0, 2),
				order = null,
				type = ActorType.Rat,
				pos = pos
			};

			SpawnActor(a);
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

		m_logic.Update();

		foreach (var actor in actors)
		{
			Vec3 prevPos = actor.pos;
			bool anyChange = actor.Step(m_lookup, terrain);

			if (anyChange)
			{
				if (prevPos != actor.pos)
				{
					m_lookup.Remove(terrain.GetIdx(prevPos));
					m_lookup.Add(terrain.GetIdx(actor.pos), actor);
				}

				change.actors.Add(actor);
			}
		}

		foreach (var a in m_newPlayers)
		{
			change.actors.Add(a);
		}
		m_newPlayers.Clear();

		return change;
	}
}
