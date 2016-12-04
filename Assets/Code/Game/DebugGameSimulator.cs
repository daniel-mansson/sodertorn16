using UnityEngine;
using System.Collections;

public class DebugGameSimulator : MonoBehaviour
{
	public SurvivalGameView m_gameView;

	public SurvivalGame m_game;
	public int mapSize;
	public int m_myPlayerId;

	void Start ()
	{
	
	}

	float timer = 0;

	void Update ()
	{
		if (m_game != null && m_game.terrain.size != 0)
		{
			timer += Time.deltaTime;
			if (timer > 0.5f)
			{
				timer = 0;
				var change = m_game.Step();
				m_gameView.UpdateState(change);
			}
		}
	}

	void OnGUI()
	{
		Rect r = new Rect(20, 20, 100, 40);

		if (GUI.Button(r, "Recreate"))
		{
			m_game = new SurvivalGame(new SurvivalGameConfig()
			{
				mapSize = mapSize,
				seed = Random.Range(1, 1000000)
			});

			m_gameView.Init(m_game.GenerateStartState());
		}


		r.y += r.height;
		if (GUI.Button(r, "Join"))
		{
			m_myPlayerId = m_game.JoinGame("Daniel");
			m_gameView.SetLocalPlayerId(m_myPlayerId);
		}
	}
}
