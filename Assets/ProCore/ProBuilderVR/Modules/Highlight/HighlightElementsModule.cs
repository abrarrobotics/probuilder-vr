﻿using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using ProBuilder2.Common;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace ProBuilder2.VR
{
	/**
	 * Set face and edge highlights on selected objects.
	 */
	public class HighlightElementsModule : MonoBehaviour
	{
		[SerializeField]
		private Material m_HighlightMaterial;
		private readonly Dictionary<pb_Object, Mesh> m_Highlights = new Dictionary<pb_Object, Mesh>();

		void LateUpdate()
		{
			foreach (var kvp in m_Highlights)
			{
				if (kvp.Key == null)
					continue;

				Graphics.DrawMesh(kvp.Value, kvp.Key.gameObject.transform.localToWorldMatrix, m_HighlightMaterial, kvp.Key.gameObject.layer, null, 0);
			}
		}

		void OnDestroy()
		{
			List<Mesh> m = m_Highlights.Values.ToList();
			for(int i = m.Count - 1; i > -1; i--)
				U.Object.Destroy(m[i]);
			m_Highlights.Clear();
		}

		/**
		 * Highlight a set of faces on a pb_Object.
		 */
		public void SetFaceHighlight(pb_Object pb, pb_Face[] faces)
		{
			if(pb == null)
				return;

			Mesh m = null;

			if(!m_Highlights.TryGetValue(pb, out m))
			{
				if(faces != null)
				{
					if(m == null)
						m = new Mesh();

					m.Clear();
					m.vertices	= pb.vertices;
					m_Highlights.Add(pb, m);
				}
				else
				{
					return;
				}
			}
			else if(faces == null)
			{
				Mesh t = m;
				m_Highlights.Remove(pb);
				U.Object.Destroy(t);
				return;
			}

			m.triangles = pb_Face.AllTriangles(faces);

			Vector3[] normals = new Vector3[pb.vertexCount];

			for(int i = 0; i < faces.Length; i++)
			{
				pb_Face face = faces[i];

				Vector3 normal = pb_Math.Normal(pb, face);

				for(int n = 0; n < face.distinctIndices.Length; n++)
					normals[face.distinctIndices[n]] = normal;
			}

			m.normals = normals;
		}

		/**
		 * Update the vertex positions for a pb_Object in the selection.
		 */
		public void UpdateVertices(pb_Object pb)
		{
			Mesh m = null;

			if(!m_Highlights.TryGetValue(pb, out m))
				return;
				
			m.vertices = pb.vertices;
		}
	}
}