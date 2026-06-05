using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AZUL
{
    public class CameraMovement : MonoBehaviour
    {
        [Header("移动设置")]
        [SerializeField]
        private float m_MoveSpeed = 40f;

        [Header("缩放设置")]
        [SerializeField]
        private float m_ScrollSpeed = 10f;

        [SerializeField]
        private float m_MinOrthoSize = 1f;

        [SerializeField]
        private float m_MaxOrthoSize = 20f;

        [Header("旋转设置")]
        [SerializeField]
        private float m_RotationSpeed = 400f;

        [SerializeField]
        private float m_MinPitch = -80f;

        [SerializeField]
        private float m_MaxPitch = 80f;

        private Camera m_Camera;
        private float m_CurrentPitch = 0f;
        private float m_CurrentYaw = 0f;

        private void Start()
        {
            m_Camera = GetComponent<Camera>() ?? Camera.main;

            // 初始化当前的旋转角度
            Vector3 currentRotation = transform.eulerAngles;
            m_CurrentPitch = currentRotation.x;
            m_CurrentYaw = currentRotation.y;

            // 规范化俯仰角到 [-180, 180] 范围
            if (m_CurrentPitch > 180f)
            {
                m_CurrentPitch -= 360f;
            }
        }

        private void Update()
        {
            HandleMovement();
            HandleZoom();
            HandleRotation();
        }

        /// <summary>
        /// 处理相机移动（WASD + EQ）
        /// </summary>
        private void HandleMovement()
        {
            Vector3 move = Vector3.zero;

            // WASD 控制水平移动
            if (Input.GetKey(KeyCode.W))
            {
                move += Vector3.forward;
            }
            if (Input.GetKey(KeyCode.S))
            {
                move += Vector3.back;
            }
            if (Input.GetKey(KeyCode.A))
            {
                move += Vector3.left;
            }
            if (Input.GetKey(KeyCode.D))
            {
                move += Vector3.right;
            }

            // EQ 控制上下移动
            Vector3 verticalMove = Vector3.zero;
            if (Input.GetKey(KeyCode.E))
            {
                verticalMove += Vector3.up;
            }
            if (Input.GetKey(KeyCode.Q))
            {
                verticalMove += Vector3.down;
            }

            // 处理水平移动
            if (move != Vector3.zero)
            {
                // 根据相机当前朝向移动
                Vector3 forward = transform.forward;
                Vector3 right = transform.right;

                // 去除 Y 轴分量，保持水平移动
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                Vector3 worldMove = (forward * move.z + right * move.x) * m_MoveSpeed * Time.deltaTime;
                transform.position += worldMove;
            }

            // 处理垂直移动
            if (verticalMove != Vector3.zero)
            {
                transform.position += verticalMove * m_MoveSpeed * Time.deltaTime;
            }
        }

        /// <summary>
        /// 处理相机缩放（滚轮）
        /// </summary>
        private void HandleZoom()
        {
            if (m_Camera != null && m_Camera.orthographic)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0f)
                {
                    float newSize = m_Camera.orthographicSize - scroll * m_ScrollSpeed;
                    m_Camera.orthographicSize = Mathf.Clamp(newSize, m_MinOrthoSize, m_MaxOrthoSize);
                }
            }
            else if (m_Camera != null && !m_Camera.orthographic)
            {
                // 透视相机模式下，通过移动相机来实现缩放效果
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0f)
                {
                    Vector3 forward = transform.forward;
                    transform.position += forward * scroll * m_ScrollSpeed;
                }
            }
        }

        /// <summary>
        /// 处理相机旋转（鼠标右键）
        /// </summary>
        private void HandleRotation()
        {
            // 鼠标右键按下时旋转视角
            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");

                // 更新偏航角（左右旋转）
                m_CurrentYaw += mouseX * m_RotationSpeed * Time.deltaTime;

                // 更新俯仰角（上下旋转）
                m_CurrentPitch -= mouseY * m_RotationSpeed * Time.deltaTime;
                m_CurrentPitch = Mathf.Clamp(m_CurrentPitch, m_MinPitch, m_MaxPitch);

                // 应用旋转
                transform.eulerAngles = new Vector3(m_CurrentPitch, m_CurrentYaw, 0f);
            }
        }
    }
}
