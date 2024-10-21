using UnityEngine;

public struct Triangle 
{
    public Vector3 v1;
    public Vector3 v2;
    public Vector3 v3;

    public Vector3 GetNormal()
    {
        return Vector3.Cross(v1 - v2, v1 - v3).normalized;
    }

    // Convert direction to point in the direction of the tri
    // 점의 방향을 삼각형의 방향으로 바꾼다
    public void MatchDirection(Vector3 dir)
    {
        if (Vector3.Dot(GetNormal(), dir) > 0) 
        {
            return;
        }

        (v1, v3) = (v3, v1);
    }
}