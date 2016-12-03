using UnityEngine;
using System.Collections;

public class ActorView : MonoBehaviour
{
	const float seqTime = 0.5f;

	public ActorType m_type;
	public AnimationCurve m_jumpCurve;
	public float m_moveSpeedFactor = 1f;
	public float m_rotSpeedFactor = 1f;
	Actor m_actor;

	int targetDir = 0;
	Vec3 targetPos;

	public virtual void Init(Actor actor)
	{
		m_actor = actor;

		transform.position = new Vector3(m_actor.pos.x, m_actor.pos.y, m_actor.pos.z);
		transform.rotation = Quaternion.Euler(0f, m_actor.dir * 90f, 0f);

		targetPos = m_actor.pos;
		targetDir = m_actor.dir;
	}

	public virtual IEnumerator MoveSeq()
	{
		targetPos = m_actor.pos;
		float timer = 0f;

		Vector3 prevPos = transform.position;
		Vector3 newPos = new Vector3(m_actor.pos.x, m_actor.pos.y, m_actor.pos.z);

		while (timer < seqTime)
		{
			timer += Time.deltaTime * m_moveSpeedFactor;
			float t = timer / seqTime;
			transform.position = Vector3.Lerp(prevPos, newPos, t) + Vector3.up * m_jumpCurve.Evaluate(t);
			yield return null;
		}

		transform.position = newPos;
	}

	public virtual IEnumerator RotSeq()
	{
		targetDir = m_actor.dir;
		float timer = 0f;

		Quaternion prevRot = transform.rotation;
		Quaternion newRot = Quaternion.Euler(0f, m_actor.dir * 90f, 0f);

		while (timer < seqTime)
		{
			timer += Time.deltaTime * m_rotSpeedFactor;
			float t = timer / seqTime;
			transform.rotation = Quaternion.Slerp(prevRot, newRot, t);
			yield return null;
		}

		transform.rotation = newRot;
	}

	public virtual void UpdateActor()
	{
		if (targetPos.x != m_actor.pos.x ||
			targetPos.y != m_actor.pos.y ||
			targetPos.z != m_actor.pos.z)
		{
			StartCoroutine(MoveSeq());
		}

		if (targetDir != m_actor.dir)
		{
			StartCoroutine(RotSeq());
		}
	}

	public virtual void Kill()
	{
		Destroy(this.gameObject);
	}
}
