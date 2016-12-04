using UnityEngine;
using System.Collections;

public class CameraRig : MonoBehaviour
{
	public Transform targetTransform;
	public float speed;
	Vector3 target;

	public void SetAndTele(Transform targetTrans)
	{
		targetTransform = targetTrans;
		Teleport(targetTrans.position);
	}

	public void Teleport(Vector3 pos)
	{
		target = pos;
		transform.position = pos;
	}

	void Start()
	{
		target = transform.position;
	}

	void Update ()
	{
		if (targetTransform != null)
			target = targetTransform.position;

		transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * speed);
	}
}
