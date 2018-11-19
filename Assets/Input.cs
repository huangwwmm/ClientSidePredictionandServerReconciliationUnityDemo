using UnityEngine;
using System.Collections;

public class InputManager : MonoBehaviour
{
	private Vector2 m_Asix = Vector2.zero;

	private bool m_QKeyDown = false;

	public Vector2 GetAsix()
	{
		return m_Asix;
	}

	public bool GetQKeyDown()
	{
		return m_QKeyDown;
	}

	protected void LateUpdate()
	{
		m_Asix.x = Input.GetAxis("Horizontal");
		m_Asix.y = Input.GetAxis("Vertical");

		m_QKeyDown = Input.GetKeyDown(KeyCode.Q);
	}

}