using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class MeshBreak : MonoBehaviour
{
    private List<GameObject> meshList = new List<GameObject>();

    public float range;
    public int pieceNum;
    
    private void Start()
    {
        GameObject[] tmp = GameObject.FindGameObjectsWithTag("Slicable");

        for (int i = 0; i < tmp.Length; i++)
        {
            meshList.Add(tmp[i]);
        }

        BreakMesh();
    }
    
    private void BreakMesh()
    {
        for (int i = 1; i < pieceNum; ++i)
        {
            Slice(meshList[Random.Range(0, meshList.Count)]);
        }
    }

    void Slice(GameObject mesh)
    {
        //오브젝트 트랜스폼
        Transform tr = mesh.transform;

        float rangeX = tr.position.x;
        float rangeY = tr.position.y;
        float rangeZ = tr.position.z;

        Vector3[] vectors = new Vector3[3];

        // 지정된 범위 내에서 랜덤한 vector 3개 생성
        for (int i = 0; i < vectors.Length; i++)
        {
            vectors[i] = new Vector3(Random.Range(rangeX - range, rangeX + range),
                Random.Range(rangeY - range, rangeY + range),
                Random.Range(rangeZ - range, rangeZ + range));
        }

        //생성된 랜덤한 vector 3개로 새로운 평면 생성
        Plane pl = new Plane(vectors[0], vectors[1], vectors[2]);

        //오브젝트의 메쉬
        Mesh m = mesh.gameObject.GetComponent<MeshFilter>().mesh;
        //메쉬의 폴리곤과 정점
        int[] triangles = m.triangles;
        Vector3[] verts = m.vertices;

        //교차점 리스트(오브젝트)
        List<Vector3> intersections = new List<Vector3>();
        //새로운 오브젝트들의 폴리곤 정보
        List<Triangle> newTris1 = new List<Triangle>();
        List<Triangle> newTris2 = new List<Triangle>();

        // Loop through tris
        // 하나의 폴리곤와 plane의 교차점을 계산하는 것을 모든 폴리곤만큼 반복한다.
        for (int i = 0; i < triangles.Length; i += 3)
        {
            //교차점 리스트(폴리곤)
            List<Vector3> points = new List<Vector3>();

            int v1 = triangles[i];
            int v2 = triangles[i + 1];
            int v3 = triangles[i + 2];
            Vector3 p1 = tr.TransformPoint(verts[v1]);
            Vector3 p2 = tr.TransformPoint(verts[v2]);
            Vector3 p3 = tr.TransformPoint(verts[v3]);
            //벡터의 외적
            Vector3 norm = Vector3.Cross(p1 - p2, p1 - p3);
            
            // 하나의 폴리곤과 plan의 교차점이 있다면 그 교차점을 교차점들의 리스트에 추가한다.
            float ent;
            Vector3 dir = p2 - p1;
            if (pl.Raycast(new Ray(p1, dir), out ent) && ent <= dir.magnitude)
            {
                Vector3 intersection = p1 + ent * dir.normalized;
                intersections.Add(intersection);
                points.Add(intersection);
            }

            dir = p3 - p2;
            if (pl.Raycast(new Ray(p2, dir), out ent) && ent <= dir.magnitude)
            {
                Vector3 intersection = p2 + ent * dir.normalized;
                intersections.Add(intersection);
                points.Add(intersection);
            }

            dir = p3 - p1;
            if (pl.Raycast(new Ray(p1, dir), out ent) && ent <= dir.magnitude)
            {
                Vector3 intersection = p1 + ent * dir.normalized;
                intersections.Add(intersection);
                points.Add(intersection);
            }

            // Group tris and create new tris
            // 교차점이 있을 때
            if (points.Count > 0)
            {
                Debug.Assert(points.Count == 2);
                List<Vector3> points1 = new List<Vector3>();
                List<Vector3> points2 = new List<Vector3>();
                // Intersection verts
                // 교차하는 정점들을 points1과 points2 리스트에 저장
                points1.AddRange(points);
                points2.AddRange(points);
                // 정점이 평면이 가른 두 공간 중 어디에 위치하는지 확인 
                // 평면이 바라보는 쪽을 보고있으면 points1에 저장, 아니라면 points2에 저장
                if (pl.GetSide(p1))
                {
                    points1.Add(p1);
                }
                else
                {
                    points2.Add(p1);
                }

                if (pl.GetSide(p2))
                {
                    points1.Add(p2);
                }
                else
                {
                    points2.Add(p2);
                }

                if (pl.GetSide(p3))
                {
                    points1.Add(p3);
                }
                else
                {
                    points2.Add(p3);
                }

                // 정점들을 Triangle 구조체로 변환 후 newTris1에 추가한다.
                if (points1.Count == 3)
                {
                    Triangle tri = new Triangle() { v1 = points1[1], v2 = points1[0], v3 = points1[2] };
                    tri.MatchDirection(norm);
                    newTris1.Add(tri);
                }
                else
                {
                    Debug.Assert(points1.Count == 4);
                    if (Vector3.Dot((points1[0] - points1[1]), points1[2] - points1[3]) >= 0)
                    {
                        Triangle tri = new Triangle() { v1 = points1[0], v2 = points1[2], v3 = points1[3] };
                        tri.MatchDirection(norm);
                        newTris1.Add(tri);
                        tri = new Triangle() { v1 = points1[0], v2 = points1[3], v3 = points1[1] };
                        tri.MatchDirection(norm);
                        newTris1.Add(tri);
                    }
                    else
                    {
                        Triangle tri = new Triangle() { v1 = points1[0], v2 = points1[3], v3 = points1[2] };
                        tri.MatchDirection(norm);
                        newTris1.Add(tri);
                        tri = new Triangle() { v1 = points1[0], v2 = points1[2], v3 = points1[1] };
                        tri.MatchDirection(norm);
                        newTris1.Add(tri);
                    }
                }

                // 교차점으로 폴리곤을 만들어 newTris2에 저장한다.
                if (points2.Count == 3)
                {
                    Triangle tri = new Triangle() { v1 = points2[1], v2 = points2[0], v3 = points2[2] };
                    tri.MatchDirection(norm);
                    newTris2.Add(tri);
                }
                else
                {
                    Debug.Assert(points2.Count == 4);
                    if (Vector3.Dot((points2[0] - points2[1]), points2[2] - points2[3]) >= 0)
                    {
                        Triangle tri = new Triangle() { v1 = points2[0], v2 = points2[2], v3 = points2[3] };
                        tri.MatchDirection(norm);
                        newTris2.Add(tri);
                        tri = new Triangle() { v1 = points2[0], v2 = points2[3], v3 = points2[1] };
                        tri.MatchDirection(norm);
                        newTris2.Add(tri);
                    }
                    else
                    {
                        Triangle tri = new Triangle() { v1 = points2[0], v2 = points2[3], v3 = points2[2] };
                        tri.MatchDirection(norm);
                        newTris2.Add(tri);
                        tri = new Triangle() { v1 = points2[0], v2 = points2[2], v3 = points2[1] };
                        tri.MatchDirection(norm);
                        newTris2.Add(tri);
                    }
                }
            }
            else //교차점이 없을 때
            {
                if (pl.GetSide(p1))
                {
                    newTris1.Add(new Triangle() { v1 = p1, v2 = p2, v3 = p3 });
                }
                else
                {
                    newTris2.Add(new Triangle() { v1 = p1, v2 = p2, v3 = p3 });
                }
            }
        }

        // 잘라진 오브젝트의 크기가 너무 작으면 함수 중단
        if (newTris1.Count < triangles.Length / (pieceNum * 2) || newTris2.Count < triangles.Length / (pieceNum * 2))
        {
            return;
        }

        // 무게중심 계산 (잘린 단면의 폴리곤을 생성하기 위함)
        if (intersections.Count > 1)
        {
            // Sets center
            Vector3 center = Vector3.zero;
            foreach (Vector3 vec in intersections)
            {
                center += vec;
            }

            center /= intersections.Count;
            for (int i = 0; i < intersections.Count; i++)
            {
                Triangle tri = new Triangle()
                {
                    v1 = intersections[i], v2 = center,
                    v3 = i + 1 == intersections.Count ? intersections[i] : intersections[i + 1]
                };
                tri.MatchDirection(-pl.normal);
                newTris1.Add(tri);
            }

            for (int i = 0; i < intersections.Count; i++)
            {
                Triangle tri = new Triangle()
                {
                    v1 = intersections[i], v2 = center,
                    v3 = i + 1 == intersections.Count ? intersections[i] : intersections[i + 1]
                };
                tri.MatchDirection(pl.normal);
                newTris2.Add(tri);
            }
        }

        // 교차점이 있으면 폴리곤 데이터를 이용해서 sliced된 새로운 오브젝트 2개를 만든다
        if (intersections.Count > 0)
        {
            // Creates new meshes
            Material mat = mesh.gameObject.GetComponent<MeshRenderer>().material;
            Destroy(mesh.gameObject);
            meshList.Remove(mesh);

            Mesh mesh1 = new Mesh();
            Mesh mesh2 = new Mesh();

            List<Vector3> tris = new List<Vector3>();
            List<int> indices = new List<int>();

            int index = 0;
            foreach (Triangle thing in newTris1)
            {
                tris.Add(thing.v1);
                tris.Add(thing.v2);
                tris.Add(thing.v3);
                indices.Add(index++);
                indices.Add(index++);
                indices.Add(index++);
            }

            mesh1.vertices = tris.ToArray();
            mesh1.triangles = indices.ToArray();

            index = 0;
            tris.Clear();
            indices.Clear();
            foreach (Triangle thing in newTris2)
            {
                tris.Add(thing.v1);
                tris.Add(thing.v2);
                tris.Add(thing.v3);
                indices.Add(index++);
                indices.Add(index++);
                indices.Add(index++);
            }

            mesh2.vertices = tris.ToArray();
            mesh2.triangles = indices.ToArray();

            mesh1.RecalculateNormals();
            mesh1.RecalculateBounds();
            mesh2.RecalculateNormals();
            mesh2.RecalculateBounds();

            // Create new objects

            GameObject go1 = new GameObject();
            GameObject go2 = new GameObject();

            MeshFilter mf1 = go1.AddComponent<MeshFilter>();
            mf1.mesh = mesh1;
            MeshRenderer mr1 = go1.AddComponent<MeshRenderer>();
            mr1.material = mat;
            MeshCollider mc1 = go1.AddComponent<MeshCollider>();
            //if (mf1.mesh.vertexCount <= 255)
            //{
            mc1.convex = true;
            mc1.isTrigger = true;
            go1.AddComponent<Rigidbody>();
            go1.GetComponent<Rigidbody>().useGravity = false;
            go1.GetComponent<Rigidbody>().velocity = Vector3.zero;
            //}
            mc1.sharedMesh = mesh1;
            go1.tag = "Slicable";

            MeshFilter mf2 = go2.AddComponent<MeshFilter>();
            mf2.mesh = mesh2;
            MeshRenderer mr2 = go2.AddComponent<MeshRenderer>();
            mr2.material = mat;
            MeshCollider mc2 = go2.AddComponent<MeshCollider>();
            //if (mf2.mesh.vertexCount <= 255)
            //{
            mc2.convex = true;
            mc2.isTrigger = true;
            go2.AddComponent<Rigidbody>();
            go2.GetComponent<Rigidbody>().useGravity = false;
            go2.GetComponent<Rigidbody>().velocity = Vector3.zero;
            //}
            mc2.sharedMesh = mesh2;
            go2.tag = "Slicable";

            meshList.Add(go1);
            meshList.Add(go2);
            //go1.name = "PuzzlePart" + boneIndex;
            //go2.name = "PuzzlePart" + (BoneList.Count - 1);
        }
    }
}
