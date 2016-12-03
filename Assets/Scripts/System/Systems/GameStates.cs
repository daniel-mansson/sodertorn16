using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;


public class BootState : GameStateBase
{
	public override State State
	{
		get { return State.Boot; }
	}

	public override void Begin(State previousState)
	{
		if (!IsSceneLoaded("boot"))
		{
			SceneManager.LoadScene("boot");
		}
	}

	float m_delay = 4f;
	float m_timer = 0f;

	public override void Update()
	{
		if (m_timer < m_delay)
		{
			m_timer += Time.deltaTime;

			if (Input.anyKeyDown)
				m_timer += 1000f;

			if (m_timer >= m_delay)
			{
				Systems.Instance.State.QueueState(State.Start);
			}
		}
	}
}

public class StartState : GameStateBase
{
	public override State State
	{
		get { return State.Start; }
	}

	public override void Begin(State previousState)
	{
		if (!IsSceneLoaded("start"))
		{
			SceneManager.LoadScene("start");
		}
	}

	public override void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Application.Quit();
		}

		if (Input.anyKeyDown)
		{
			Systems.Instance.State.QueueState(State.Game);
		}

		int c = Systems.Instance.Input.GetControllerCount();
		for (int i = 0; i < c; i++)
		{
			if (Systems.Instance.Input.GetController(i).GetButtonDown(Xbox360ControllerButtonId.Start))
			{
				Systems.Instance.State.QueueState(State.Game);
			}
		}
	}
}

public class GameState : GameStateBase
{
	public override State State
	{
		get { return State.Game; }
	}

	public override void Begin(State previousState)
	{
		if (!IsSceneLoaded("game"))
		{
			SceneManager.LoadScene("game");
		}

		Systems.Instance.Audio.PlayMusic("music1", false);
	}

	public override void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Systems.Instance.State.QueueState(State.Start);
		}
	}
}

public class WinState : GameStateBase
{
	public override State State
	{
		get { return State.Win; }
	}

	public override void Begin(State previousState)
	{
		if (!IsSceneLoaded("win"))
		{
			SceneManager.LoadScene("win");
		}
	}

	public override void Update()
	{
		if (Input.anyKeyDown)
		{
			Systems.Instance.State.QueueState(State.Start);
		}
	}
}
