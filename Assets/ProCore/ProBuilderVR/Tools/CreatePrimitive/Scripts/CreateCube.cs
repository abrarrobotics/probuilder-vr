using UnityEngine;
using ProBuilder2.Common;
using ProBuilder2.MeshOperations;

namespace ProBuilder2.VR
{
	public class CreateCube : AShapeCreator
	{
		const float MIN_SIZE = Snapping.DEFAULT_INCREMENT;

		private Vector3[] template = new Vector3[8];
		private Vector3[] positions = new Vector3[24];
		private Vector3 m_StartPoint, m_EndPoint, m_BaseEndPoint, m_Size;
		private Plane m_Plane;
		private bool m_FacesReversed = false;
		private static readonly Vector3 VECTOR3_ONE = Vector3.one;

		enum State
		{
			Base,
			Height
		}

		State state = State.Base;

		public override bool HandleStart(Ray ray, Plane drawPlane)
		{
			m_Plane = drawPlane;

			if(!VRMath.GetPointOnPlane(ray, m_Plane, out m_StartPoint))
				return false;

			m_EndPoint = Snapping.Snap(m_StartPoint, m_SnapIncrement, VECTOR3_ONE);
			m_StartPoint = m_EndPoint;

			m_Mesh = pb_ShapeGenerator.CubeGenerator(VECTOR3_ONE);

			m_Mesh.gameObject.GetComponent<MeshRenderer>().sharedMaterial = pb_Constant.DefaultMaterial;

			foreach(pb_Face face in m_Mesh.faces)
				face.uv.useWorldSpace = true;

			m_GameObject = m_Mesh.gameObject;

			// we'll place vertex positions in world space while drawing
			m_GameObject.transform.position = Vector3.zero;

			m_Size.x = MIN_SIZE;
			m_Size.y = MIN_SIZE;
			m_Size.z = MIN_SIZE;

			UpdateShape();

			return true;
		}

		public override bool HandleDrag(Ray ray, ref Vector3 planeIntersection)
		{
			if(state == State.Base)
			{
				VRMath.GetPointOnPlane(ray, m_Plane, out planeIntersection);
				m_EndPoint = planeIntersection;
				m_EndPoint = Snapping.Snap(m_EndPoint, m_SnapIncrement, VECTOR3_ONE);
			}
			else
			{
				planeIntersection = VRMath.CalculateNearestPointRayRay(ray.origin, ray.direction, m_BaseEndPoint, m_Plane.normal);
				Vector3 dir = planeIntersection - m_BaseEndPoint;
				dir.Normalize();
				float m = Vector3.Dot(m_Plane.normal, dir) / 1f;
				float distance = Vector3.Distance(planeIntersection, m_BaseEndPoint) * m;
				distance = Snapping.Snap(distance, m_SnapIncrement);
				m_EndPoint = m_BaseEndPoint + (m_Plane.normal * distance);
			}

			UpdateShape();

			// @todo sanity check ray input to prevent gigantic shapes
			return true;
		}

		public override bool HandleTriggerRelease(Ray ray)
		{
			if(state == State.Base)
			{
				state = State.Height;
				m_BaseEndPoint = m_EndPoint;
				return true;
			}
			else
			{
				m_Mesh.CenterPivot(new int[] { 0 });
				m_Mesh.gameObject.GetComponent<MeshRenderer>().sharedMaterial = pb_Constant.DefaultMaterial;
				state = State.Base;
				m_FacesReversed = false;
				return false;
			}
		}

		private void UpdateShape()
		{     
			/**
			 * How pb_Constant.CUBE_VERTICES arranges the template
			 *
			 *     4-------5 
			 *    /       /|
			 *   /       / |
			 *  7-------6  1   
			 *  |       | /
			 *  |       |/
			 *  3-------2
			 *
			 */

			Vector3 size = m_EndPoint - m_StartPoint;

			if(m_Size != size)
			{
				m_Size = size;
				
				if(onShapeChanged != null)
					onShapeChanged(m_StartPoint + m_Size);
			}
			else
			{
				return;
			}

			bool isFlipped = !((m_Size.x < 0 ^ m_Size.y < 0) ^ m_Size.z < 0);

			if(isFlipped != m_FacesReversed)
			{
				m_FacesReversed = isFlipped;
				m_Mesh.ReverseWindingOrder(m_Mesh.faces);
			}

			// PhysX throws errors like dolla dolla bills if fed a zera space mesh
			Vector3 scale = MakeNotZero(m_Size);

			template[0] = m_StartPoint;
			template[1] = m_StartPoint + Vector3.Scale(new Vector3(1f, 0f, 0f), scale);
			template[2] = m_StartPoint + Vector3.Scale(new Vector3(1f, 0f, 1f), scale);
			template[3] = m_StartPoint + Vector3.Scale(new Vector3(0f, 0f, 1f), scale);

			template[4] = m_StartPoint + Vector3.Scale(new Vector3(0f, 1f, 0f), scale);
			template[5] = m_StartPoint + Vector3.Scale(new Vector3(1f, 1f, 0f), scale);
			template[6] = m_StartPoint + Vector3.Scale(new Vector3(1f, 1f, 1f), scale);
			template[7] = m_StartPoint + Vector3.Scale(new Vector3(0f, 1f, 1f), scale);

			int len = pb_Constant.TRIANGLES_CUBE.Length;

			for(int i = 0; i < len; i++)
				positions[i] = template[pb_Constant.TRIANGLES_CUBE[i]];

			m_Mesh.SetVertices(positions);

			m_Mesh.ToMesh();
			m_Mesh.Refresh();
		}

		private static Vector3 MakeNotZero(Vector3 v)
		{
			return new Vector3( 
				Mathf.Max(.0001f, Mathf.Abs(v.x)) * Mathf.Sign(v.x),
				Mathf.Max(.0001f, Mathf.Abs(v.y)) * Mathf.Sign(v.y),
				Mathf.Max(.0001f, Mathf.Abs(v.z)) * Mathf.Sign(v.z) );
		}
	}
}
