using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class TerrainView : MonoBehaviour
{
	public CubeView m_cubePrefab;
	public Transform m_center;
	public int m_viewRange;
	public bool m_debugCreateAll = false;

	int m_size;
	Terrain m_terrain;
	Dictionary<int, CubeView> m_activeViews = new Dictionary<int, CubeView>();

	

	void Start () {

	}

	void CreateAt(int x, int y)
	{
		if (y < 0 || x < 0 || y >= m_terrain.size || x >= m_terrain.size)
			return;

		var cube = (CubeView)Instantiate(m_cubePrefab, new Vector3(x, m_terrain.map[m_terrain.GetIdx(x, y)], y), Quaternion.identity);
		cube.transform.parent = this.transform;
		int idx = m_terrain.GetIdx(x, y);
		cube.idx = idx;
		m_activeViews.Add(idx, cube);
	}

	void DestroyAt(int x, int y)
	{
		int idx = m_terrain.GetIdx(x, y);

		if (m_activeViews.ContainsKey(idx))
		{
			var cube = m_activeViews[idx];
			Destroy(cube.gameObject);
			m_activeViews.Remove(idx);
		}
	}

	public void Init(GameStartState startState)
	{
		foreach (var v in m_activeViews.Values)
		{
			Destroy(v.gameObject);
		}
		m_activeViews.Clear();

		m_size = startState.config.mapSize;
		m_terrain = new Terrain(startState.config);
		m_terrain.Generate(startState.config.seed);

		foreach (var change in startState.change.terrainChange)
		{
			ApplyChange(change);
		}
	}

	Dictionary<int, int> m_remove = new Dictionary<int, int>();
	Dictionary<int, int> m_add = new Dictionary<int, int>();
	int lastCenterIdx = -1;
	public void UpdateCenter()
	{
		if (m_terrain == null)
			return;

		int cx = (int)(m_center.position.x + 0.5f);
		int cy = (int)(m_center.position.z + 0.5f);
		int cIdx = m_terrain.GetIdx(cx, cy);

		if (cIdx != lastCenterIdx)
		{
			m_add.Clear();
			m_remove.Clear();

			for (int i = -m_viewRange; i <= m_viewRange; i++)
			{
				for (int j = -m_viewRange; j <= m_viewRange; j++)
				{
					int x = m_terrain.GetX(lastCenterIdx) + j;
					int y = m_terrain.GetY(lastCenterIdx) + i;
					int idx = m_terrain.GetIdx(x, y);

					bool hasCube = m_activeViews.ContainsKey(idx);
					if (hasCube)
					{
						m_remove.Add(idx, idx);
					}
				}
			}

			for (int i = -m_viewRange; i <= m_viewRange; i++)
			{
				for (int j = -m_viewRange; j <= m_viewRange; j++)
				{
					int x = m_terrain.GetX(cIdx) + j;
					int y = m_terrain.GetY(cIdx) + i;
					int idx = m_terrain.GetIdx(x, y);

					bool hasCube = m_activeViews.ContainsKey(idx);
					if (hasCube)
					{
						if (m_remove.ContainsKey(idx))
							m_remove.Remove(idx);
					}
					else
					{
						m_add.Add(idx, idx);
					}
				}
			}

			foreach (var idx in m_remove.Keys)
			{
				DestroyAt(m_terrain.GetX(idx), m_terrain.GetY(idx));
			}

			foreach (var idx in m_add.Keys)
			{
				CreateAt(m_terrain.GetX(idx), m_terrain.GetY(idx));
			}

			lastCenterIdx = cIdx;
		}
	}

	public void ApplyChange(TerrainChange change)
	{
		if (m_activeViews.ContainsKey(change.idx))
		{
			var trans = m_activeViews[change.idx].transform;
			var lp = trans.localPosition;
			lp.y = change.height;
			trans.localPosition = lp;
		}
	}

	void Update ()
	{
		UpdateCenter();
	}
}
