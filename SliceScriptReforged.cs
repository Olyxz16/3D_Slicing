using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliceScriptRefined : MonoBehaviour
{
    
    GameObject target;

    Vector3[] vertices;
    List<int[]> submesh_triangles;
    List<int[]> submesh_states;
    int subMeshCount;

    float[] plane_values;
    Vector3 normal, pivot;

    List<Vector3> right_vertices, left_vertices, face_vertices;
    List<List<int>> right_triangles, left_triangles, face_triangles;
    int[] right_triangle_offset, left_triangle_offset, face_triangle_offset;

    GameObject left_go, right_go; 




    #region Slice
    public GameObject[] Slice(GameObject obj, Vector3 normal, Vector3 point, int new_layer = -1) {

        Init(obj, normal, point);
        Compute_Plane();
        //Compute_Mesh_Vertices(); 
        Compute_Mesh_States(); 

        Compute_New_Triangles();
        Inset_Faces();
        Create_New_Objects(new_layer);

        Destroy(obj);

        return new GameObject[] {left_go, left_go};

    }
    public GameObject[] Slice(GameObject obj) {
        return Slice(obj, transform.right, transform.position);
    }
    #endregion


    #region Initialization
    void Init(GameObject _obj, Vector3 _normal, Vector3 _pivot) {

        target = _obj;
        if(target.GetComponent<Collider>())
            target.GetComponent<Collider>().enabled = false;

        normal = _normal;
        pivot = _pivot;

        Mesh mesh = target.GetComponent<MeshFilter>().mesh;
        subMeshCount = mesh.subMeshCount;
        vertices = mesh.vertices;
        for(int i = 0 ; i < vertices.Length ; i++)
            vertices[i] = target.transform.TransformPoint(vertices[i]);

        // POSSIBLEMENT A SUPPRIMER
        /*for(int i = 0 ; i < mesh_vertices.Length ; i++) {
            Vector3 v = mesh_vertices[i];
            v = new Vector3(v.x * target.transform.localScale.x, v.y * target.transform.localScale.y, v.z * target.transform.localScale.z);
            v = target.transform.rotation * v;
            v = v + target.transform.position;       
            mesh_vertices[i] = new Vector3(v.x, v.y, v.z);
        }*/

        submesh_triangles = new List<int[]>();
        for(int i = 0 ; i < subMeshCount ; i++)
            submesh_triangles.Add(mesh.GetTriangles(i));

        submesh_states = new List<int[]>();

        right_vertices = new List<Vector3>();
        left_vertices = new List<Vector3>();
        face_vertices = new List<Vector3>();
        right_triangles = new List<List<int>>();
        left_triangles = new List<List<int>>();
        face_triangles = new List<List<int>>();
        right_triangle_offset = new int[subMeshCount];
        left_triangle_offset = new int[subMeshCount];
        face_triangle_offset = new int[subMeshCount];


    }
    void Compute_Plane() {
        plane_values = new float[4] {
            -normal.x,
            -normal.y,
            -normal.z,
            normal.x*pivot.x + normal.y*pivot.y + normal.z*pivot.z
        };
    }
    void Compute_Mesh_States() {
        for(int i = 0 ; i < subMeshCount ; i++) {
            int[] state = new int[submesh_triangles[i].Length];
            for(int j = 0 ; j < state.Length ; j++) 
                state[j] = (int)Mathf.Sign(Solve(vertices[submesh_triangles[i][j]]));
            submesh_states.Add(state);
        }
    }
    #endregion



    #region Triangles
    void Compute_New_Triangles() {
        for(int i = 0 ; i < subMeshCount ; i++) {
            right_triangles.Add(new List<int>());
            left_triangles.Add(new List<int>());
            for(int j = 0 ; j < submesh_triangles[i].Length/3 ; j++) {
                Vector3[] v = new Vector3[3] {vertices[submesh_triangles[i][3*j]], vertices[submesh_triangles[i][3*j+1]], vertices[submesh_triangles[i][3*j+2]]};
                int[] s = new int[3] {submesh_states[i][3*j], submesh_states[i][3*j+1], submesh_states[i][3*j+2]};
                Compute_Triangle(v, s, i, j);
            }
        }
    }

    void Compute_Triangle(Vector3[] v, int[] s, int i, int j) {

        if(s[0] == 1 && s[1] == 1 && s[2] == 1) {
            right_vertices.AddRange(v);
            Add_Triangles_Right(i,j, j+1, j+2);
        }
        else if(s[0] == 0 && s[1] == 0 && s[2] == 0) {
            left_vertices.AddRange(v);
            Add_Triangles_Left(i,j, j+1, j+2);
        }
        
        else if(s[0] == 1 && s[1] == 1 && s[2] == 0) {                 
            Add_Vertices_Left(v[2], Inter(v[0], v[2]), Inter(v[1], v[2]));
            Add_Vertices_Right(v[1], Inter(v[1], v[2]), Inter(v[0], v[2]));
            Add_Vertices_Right(v[0], v[1], Inter(v[0], v[2]));

            face_vertices.Add(Inter(v[0], v[2]));
            face_vertices.Add(Inter(v[1], v[2]));

            Add_Triangles_Left(i,j, j+1, j+2);
            Add_Triangles_Right(i,j, j+1, j+2);
            Add_Triangles_Right(i,j, j+1, j+2);
        }
        else if(s[0] == 1 && s[1] == 0 && s[2] == 1) {          
            Add_Vertices_Left(v[1], Inter(v[1], v[2]), Inter(v[0], v[1]));
            Add_Vertices_Right(v[0], Inter(v[0], v[1]), Inter(v[1], v[2]));
            Add_Vertices_Right(Inter(v[1], v[2]), v[2], v[0]);

            face_vertices.Add(Inter(v[0], v[1]));
            face_vertices.Add(Inter(v[1], v[2]));

            Add_Triangles_Left(i,j, j+1, j+2);
            Add_Triangles_Right(i,j, j+1, j+2);
            Add_Triangles_Right(i,j, j+1, j+2); 
        } 
        else if(s[0] == 0 && s[1] == 1 && s[2] == 1) {              
            Add_Vertices_Left(v[0], Inter(v[0], v[1]), Inter(v[0], v[2]));
            Add_Vertices_Right(v[1], Inter(v[0], v[2]), Inter(v[0], v[1]));
            Add_Vertices_Right(v[1], v[2], Inter(v[0], v[2]));

            face_vertices.Add(Inter(v[0], v[1]));
            face_vertices.Add(Inter(v[0], v[2]));

            Add_Triangles_Left(i,j, j+1, j+2);
            Add_Triangles_Right(i,j, j+1, j+2);
            Add_Triangles_Right(i,j, j+1, j+2);
        }



        if(s[0] == 1 && s[1] == 0 && s[2] == 0) {           
            Add_Vertices_Right(Inter(v[0], v[1]), Inter(v[0], v[2]), v[0]);
            Add_Vertices_Left(Inter(v[0], v[2]), Inter(v[0], v[1]), v[1]);        
            Add_Vertices_Left(v[1], v[2], Inter(v[0], v[2]));

            face_vertices.Add(Inter(v[0], v[1]));
            face_vertices.Add(Inter(v[0], v[2]));

            Add_Triangles_Right(i,j, j+1, j+2);
            Add_Triangles_Left(i,j, j+1, j+2);
            Add_Triangles_Left(i,j, j+1, j+2);
        } 
        else if(s[0] == 0 && s[1] == 1 && s[2] == 0) {         
            Add_Vertices_Right(v[1], Inter(v[1], v[2]), Inter(v[0], v[1]));
            Add_Vertices_Left(v[0], Inter(v[0], v[1]), Inter(v[1], v[2]));
            Add_Vertices_Left(Inter(v[1], v[2]), v[2], v[0]);

            face_vertices.Add(Inter(v[0], v[1]));
            face_vertices.Add(Inter(v[1], v[2]));

            Add_Triangles_Right(i,j, j+1, j+2);  
            Add_Triangles_Left(i,j, j+1, j+2); 
            Add_Triangles_Left(i,j, j+1, j+2);    
        } 
        else if(s[0] == 0 && s[1] == 0 && s[2] == 1) {
            Add_Vertices_Right(v[2], Inter(v[0], v[2]), Inter(v[1], v[2]));
            Add_Vertices_Left(v[1], Inter(v[1], v[2]), Inter(v[0], v[2]));
            Add_Vertices_Left(v[0], v[1], Inter(v[0], v[2]));

            face_vertices.Add(Inter(v[0], v[2])); 
            face_vertices.Add(Inter(v[1], v[2]));

            Add_Triangles_Right(i,j, j+1, j+2);
            Add_Triangles_Left(i,j, j+1, j+2);
            Add_Triangles_Left(i,j, j+1, j+2);
        }


    }
    #endregion



    #region Inset_Face
    void Inset_Faces() {

        Vector3 centroid = Compute_Centroid();
        Remove_Duplicate_Inside_Face_Vertices();
        Sort_Inside_Vertices(centroid);
        Remove_Congruent_Vertices();

        Create_Face_Triangles(centroid);

    }


    Vector3 Compute_Centroid() {
        Vector3 centroid = Vector3.zero;
        foreach(Vector3 v in face_vertices)
            centroid += v;
        centroid /= face_vertices.Count;
        return centroid;
    }
    void Remove_Duplicate_Inside_Face_Vertices() {
        for(int i = 0 ; i < face_vertices.Count ; i++)
            for(int j = 0 ; j < face_vertices.Count ; j++)
                if(face_vertices[i] == face_vertices[j] && i != j) {
                    face_vertices.RemoveAt(i);
                    i--;
                    break;
                }
    }
    void Sort_Inside_Vertices(Vector3 centroid) {

        List<float> angle_list = new List<float>();
        List<Vector3> sorted_vertices = new List<Vector3>();

        Quaternion rot = Quaternion.FromToRotation(normal, Vector3.back);


        for(int i = 0 ; i < face_vertices.Count ; i++) {
            Vector3 dir = face_vertices[i] - centroid;
            Vector3 new_pos = rot*dir;
            new_pos = new Vector3(new_pos.y, new_pos.x, 0);
            float angle = Mathf.Atan2(new_pos.x, new_pos.y)*Mathf.Rad2Deg + 90;
            angle_list.Add(angle + (angle <= 0 ? 360 : 0));
        }

        
        int indice = angle_list.Count;
        while(indice > 0) {
            float min = float.MaxValue;
            int index = 0;
            for(int i = 0 ; i < indice ; i++)
                if(angle_list[i] < min)  {
                    min = angle_list[i];
                    index = i;
                }
            sorted_vertices.Add(face_vertices[index]);
            face_vertices.RemoveAt(index);
            angle_list.RemoveAt(index);
            indice--;
        }

        face_vertices = sorted_vertices;

    }
    void Remove_Congruent_Vertices() {
        int c = face_vertices.Count;
        for(int i = c-1 ; i > 1 ; i--) {
            if(Mathf.Abs(Vector3.Dot((face_vertices[(i-1)%c] - face_vertices[i%c]).normalized, (face_vertices[(i-2)%c] - face_vertices[i%c]).normalized)) == 1) {
                face_vertices.RemoveAt((i-1)%c);
                c--;
                i++;
            }
        }
    }
    void Create_Face_Triangles(Vector3 centroid) {

        int offr = right_vertices.Count;
        int offl = left_vertices.Count;
        
        right_vertices.Add(centroid);
        left_vertices.Add(centroid);
        right_triangles.Add(new List<int>());
        left_triangles.Add(new List<int>());

        for(int i = 0 ; i < face_vertices.Count ; i++) {
            
            right_vertices.Add(face_vertices[i]);
            left_vertices.Add(face_vertices[i]);

            if(i != face_vertices.Count) {
                Add_Triangles_Right(subMeshCount+1, offr+i, offr, offr+i+1); 
                Add_Triangles_Left(subMeshCount+1, offr+i+1, offr, offr+i); 
            }
            else {
                Add_Triangles_Right(subMeshCount+1, offr+i, offr, offr+1);
                Add_Triangles_Left(subMeshCount+1, offr+1, offr, offr+i);
            }

        }

    }
    #endregion

    #region ObjectCreation
    void Create_New_Objects(int index = 0, int layer = -1) {
            right_go = Create_New_Object(right_vertices, right_triangles, index, layer);
            left_go = Create_New_Object(left_vertices, left_triangles , index, layer);
    }
    GameObject Create_New_Object(List<Vector3> vertices, List<List<int>> triangles, int index, int layer) {

        Mesh new_mesh = new Mesh();
        for(int i = 0 ; i < vertices.Count ; i++)
            vertices[i] = target.transform.InverseTransformPoint(vertices[i]);

        new_mesh.vertices = vertices.ToArray();
        for(int i = 0 ; i < subMeshCount ; i++) {
            if(i != index)
                new_mesh.SetTriangles(triangles[i].ToArray(), i);
            else
                new_mesh.SetTriangles(Merge(triangles[i].ToArray(), triangles[subMeshCount+1].ToArray()), i);
        }

        GameObject new_object = Instantiate(target, target.transform.position, target.transform.rotation);
        new_object.GetComponent<MeshFilter>().mesh = new_mesh;
        new_object.GetComponent<MeshFilter>().mesh.RecalculateNormals();

        new_object.name = target.name + " copy";

        if(new_object.GetComponent<Collider>() && !new_object.GetComponent<MeshCollider>()) {
            Destroy(new_object.GetComponent<Collider>());
            new_object.AddComponent<MeshCollider>();
        }
        new_object.GetComponent<MeshCollider>().sharedMesh = new_mesh;
        if(new_object.GetComponent<Rigidbody>()) {
            new_object.GetComponent<MeshCollider>().convex = true;
            new_object.GetComponent<Rigidbody>().velocity = target.GetComponent<Rigidbody>().velocity;
            new_object.GetComponent<Rigidbody>().angularVelocity = target.GetComponent<Rigidbody>().angularVelocity;
            new_object.GetComponent<Rigidbody>().ResetCenterOfMass();
        }

        if(layer == -1) 
            new_object.layer = target.layer;
        else
            new_object.layer = layer;
        new_object.transform.parent = target.transform.parent;

        return new_object;

    }
    #endregion




    #region Tools
    float Solve(Vector3 point) {
        return plane_values[0]*point.x + plane_values[1]*point.y + plane_values[2]*point.z + plane_values[3];
    }
    Vector3 Inter(Vector3 p1, Vector3 p2) {     
        Vector3 dir = p2-p1;
        float t = -(plane_values[0]*p1.x +
                    plane_values[1]*p1.y + 
                    plane_values[2]*p1.z + plane_values[3]) / 
                   (plane_values[0]*dir.x + plane_values[1]*dir.y + plane_values[2]*dir.z);
        return new Vector3(
            dir.x * t + p1.x,
            dir.y * t + p1.y,
            dir.z * t + p1.z
        );
    }

    int[] Merge(int[] arr1, int[] arr2) {
        int[] new_arr = new int[arr1.Length+arr2.Length];
        for(int i = 0 ; i < arr1.Length ; i++)
            new_arr[i] = arr1[i];
        for(int i = 0 ; i < arr2.Length ; i++)
            new_arr[i+arr1.Length] = arr2[i];
        return new_arr;
    }

    void Add_Vertices_Left(Vector3 v1, Vector3 v2, Vector3 v3) {
        left_vertices.Add(v1);
        left_vertices.Add(v2);
        left_vertices.Add(v3);
    }
    void Add_Vertices_Right(Vector3 v1, Vector3 v2, Vector3 v3) {
        right_vertices.Add(v1);
        right_vertices.Add(v2);
        right_vertices.Add(v3);
    }
    void Add_Vertices_Face(Vector3 v1, Vector3 v2, Vector3 v3) {
        face_vertices.Add(v1);
        face_vertices.Add(v2);
        face_vertices.Add(v3);
    }
    void Add_Triangles_Left(int i, int t1, int t2, int t3) {
        left_triangles[i].Add(t1);
        left_triangles[i].Add(t2);
        left_triangles[i].Add(t3);
        //left_triangle_offset[i]+=3;
    }
    void Add_Triangles_Right(int i, int t1, int t2, int t3) {
        right_triangles[i].Add(t1);
        right_triangles[i].Add(t2);
        right_triangles[i].Add(t3);
        //right_triangle_offset[i]+=3;
    }
    void Add_Triangles_Face(int i, int t1, int t2, int t3) {
        face_triangles[i].Add(t1);
        face_triangles[i].Add(t2);
        face_triangles[i].Add(t3);
        //right_triangle_offset[i]+=3;
    }
    #endregion











}
