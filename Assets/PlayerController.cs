using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
	System.Action<int> m_onClickTerrain;

	public void Init(System.Action<int> onClickTerrain)
	{
		m_onClickTerrain = onClickTerrain;
	}

	void Start ()
	{
	
	}
	
	void Update ()
	{
		if (Input.GetMouseButtonDown(0))
		{
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit info;
			if (Physics.Raycast(ray, out info))
			{
				var cube = info.collider.GetComponent<CubeView>();
				if (cube != null)
				{
					if (m_onClickTerrain != null)
						m_onClickTerrain(cube.idx);
				}
			}
		}
	}
}
