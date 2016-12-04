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
	public static int ToDir(int x, int z)
	{
		if (z > 0)
			return 0;
		if (x > 0)
			return 1;
		if (z < 0)
			return 2;
		else
			return 3;
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
							dir = first.target.x;
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

	public int GetHeight(int idx)
	{
		return map[idx];
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
									target = new Vec3(m_random.Range(0, 4), 0, 0),
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
	public DeterministicRandom m_random;
	public List<Actor> m_actors;
	public Terrain m_terrain;
	public SurvivalGameConfig config;
	public int frame;
	public int nextId;

	Dictionary<int, Actor> m_lookup = new Dictionary<int, Actor>();
	Logic m_logic;
	List<Actor> m_newPlayers = new List<Actor>();

	public SurvivalGame(SurvivalGameConfig config)
	{
		this.config = config;
		m_terrain = new Terrain(config);
		m_actors = new List<Actor>();
		frame = 0;
		m_random = new DeterministicRandom((uint)config.seed);

		m_logic = new Logic(m_lookup, m_terrain, config.seed);

		GenerateWorld();
	}

	public int JoinGame(string name)
	{
		Vec3 pos = GetFreeSpot();

		var a = new Actor()
		{
			id = nextId++,
			dir = m_random.Range(0, 4),
			subType = m_random.Range(0, 100000),
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
			pos = new Vec3(m_random.Range(0, m_terrain.size), 0, m_random.Range(0, m_terrain.size));

			if (m_lookup.ContainsKey(m_terrain.GetIdx(pos)))
				continue;

			pos.y = m_terrain.GetHeight(pos);

			if (pos.y > 0)
				break;
		}
		return pos;
	}

	void SpawnActor(Actor actor)
	{
		m_actors.Add(actor);
		m_logic.Add(actor);
		m_lookup.Add(m_terrain.GetIdx(actor.pos), actor);
	}

	void KillActor(Actor actor)
	{
		m_actors.Remove(actor);
		m_logic.Remove(actor);
		m_lookup.Remove(m_terrain.GetIdx(actor.pos));
	}

	void GenerateWorld()
	{
		m_terrain.Generate(config.seed);

		for (int i = 0; i < 8; ++i)
		{
			Vec3 pos = GetFreeSpot();

			var a = new Actor()
			{
				id = nextId++,
				dir = m_random.Range(0, 4),
				subType = m_random.Range(0, 2),
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
				actors = m_actors,
				frame = frame,
				killedActors = new List<Actor>(),
				terrainChange = m_terrain.changeSinceStart
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

		foreach (var actor in m_actors)
		{
			Vec3 prevPos = actor.pos;
			bool anyChange = actor.Step(m_lookup, m_terrain);

			if (anyChange)
			{
				if (prevPos != actor.pos)
				{
					m_lookup.Remove(m_terrain.GetIdx(prevPos));
					m_lookup.Add(m_terrain.GetIdx(actor.pos), actor);
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

	public void RequestMoveOrder(int playerId, int idx)
	{
		var actor = m_actors.FirstOrDefault(a => a.id == playerId);
		if (actor != null)
		{
			actor.order = null;
			BuildMoveOrder(actor, idx);
		}
	}

	public void BuildMoveOrder(Actor actor, int targetIdx)
	{
		int cidx = m_terrain.GetIdx(actor.pos);
		int cx = m_terrain.GetX(cidx);
		int cy = m_terrain.GetY(cidx);

		int tx = m_terrain.GetX(targetIdx);
		int ty = m_terrain.GetY(targetIdx);

		List<int> path = new List<int>();
		path.Add(cidx);

		while (cx != tx || cy != ty)
		{
			int dx = tx - cx;
			int dy = ty - cy;

			int adx = Math.Abs(dx);
			int ady = Math.Abs(dy);

			if (adx > ady)
			{
				cx += Math.Sign(dx);
			}
			else if (ady > adx)
			{
				cy += Math.Sign(dy);
			}
			else
			{
				if (m_random.Range(0, 2) == 0)
				{
					cx += Math.Sign(dx);
				}
				else
				{
					cy += Math.Sign(dy);
				}
			}

			int ph = m_terrain.GetHeight(cidx);
			cidx = m_terrain.GetIdx(cx, cy);
			int ch = m_terrain.GetHeight(cidx);
			int dh = ch - ph;
			if (dh <= 1 && ch >= 0)
			{
				path.Add(cidx);
			}
			else
			{
				break;
			}
		}

		if (path.Count > 1)
		{
			List<ActorAction> actions = new List<ActorAction>();

			//Yay, we are gonna move yo
			int pidx = m_terrain.GetIdx(actor.pos);
			int px = m_terrain.GetX(cidx);
			int py = m_terrain.GetY(cidx);
			int pdir = actor.dir;

			for (int i = 1; i < path.Count; i++)
			{
				int prevIdx = path[i - 1];
				int prevX = m_terrain.GetX(path[i - 1]);
				int prevY = m_terrain.GetY(path[i - 1]);

				int idx = path[i];
				int newX = m_terrain.GetX(idx);
				int newY = m_terrain.GetY(idx);

				int newDir = Vec3.ToDir(newX - prevX, newY - prevY);
				if (newDir != pdir)
				{
					pdir = newDir;
					actions.Add(new ActorAction()
					{
						startDelay = 0,
						endDelay = 0,
						target = new Vec3(newDir,0,0),
						type = ActorActionType.Rotate
					});
				}

				actions.Add(new ActorAction()
				{
					startDelay = 0,
					endDelay = 0,
					target = new Vec3(newX, m_terrain.GetHeight(newX, newY), newY),
					type = ActorActionType.Move
				});
			}

			actor.order = new Order()
			{
				actions = actions
			};
		}
	} 
}
