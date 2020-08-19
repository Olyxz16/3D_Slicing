using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SlicerScript : MonoBehaviour
{
    

    Transform slicer;
    
    GameObject object_To_Slice;
    Transform parent;
    Mesh mesh;
    Vector3[] mesh_vertices;
    int[] mesh_triangles_indices;
    int[] mesh_state;

    float[] plane_values;

    List<Vector3> right_mesh_vertices, left_mesh_vertices;
    List<Vector3> new_vertices;
    List<int> right_mesh_triangle_indices, left_mesh_triangle_indices;
    int right_indices_offset, left_indices_offset;

    Vector3 centroid;
    Vector3 right_center_of_mass, left_center_of_mass;

    Mesh new_right_mesh, new_left_mesh;
    GameObject new_left_gameObject, new_right_gameObject;






    void Start() {

        slicer = this.transform;

    }


    /*void OnDrawGizmos() {
        
        GameObject selected = Selection.activeGameObject;
        Vector3 center = Compute_Center_Of_Mass(new List<Vector3>(selected.GetComponent<MeshFilter>().mesh.vertices));
        center = selected.transform.rotation * center;
        center = center + selected.transform.position;

        Debug.Log("oui");

        Gizmos.DrawSphere(center, 0.15f);
        
    }*/



    void OnCollisionEnter(Collision other) {
        
        if(other.gameObject.layer == LayerMask.NameToLayer("Sliceable"))
            Slice(other.gameObject);

    }




    public void Slice(GameObject obj) {
        
        //float t = Time.realtimeSinceStartup;

        Initialize_Mesh(obj);
        plane_values = Compute_Plane(slicer.right, slicer.position);
        Compute_Mesh_Vertices();
        Compute_Mesh_State();

        Compute_New_Triangles();
        Inset_Faces();
        Create_New_Objects();

        Destroy(object_To_Slice);


        //Debug.Log(Time.realtimeSinceStartup -t);

    }



    void Initialize_Mesh (GameObject obj) {  

        object_To_Slice = obj;
        parent = object_To_Slice.transform.parent;
        mesh = obj.GetComponent<MeshFilter>().mesh;

        mesh_vertices = mesh.vertices;
        for(int i = 0 ; i < mesh_vertices.Length ; i++) {
            Vector3 v = mesh_vertices[i];
            v = new Vector3(v.x * object_To_Slice.transform.localScale.x, v.y * object_To_Slice.transform.localScale.y, v.z * object_To_Slice.transform.localScale.z);
            v = object_To_Slice.transform.rotation * v;
            v = v + object_To_Slice.transform.position;
            mesh_vertices[i] = new Vector3(v.x, v.y, v.z);
        }
           
        mesh_triangles_indices = mesh.triangles;

        right_mesh_vertices = new List<Vector3>();
        left_mesh_vertices = new List<Vector3>();
        new_vertices = new List<Vector3>();

        right_mesh_triangle_indices = new List<int>();
        left_mesh_triangle_indices = new List<int>();

        right_indices_offset = 0;
        left_indices_offset = 0;

    }

    float[] Compute_Plane(Vector3 normal, Vector3 point) {    
        return new float[4] {
            -normal.x,
            -normal.y,
            -normal.z,
            normal.x*point.x + normal.y*point.y + normal.z*point.z
        };
    }


    void Compute_Mesh_Vertices() {

        Vector3[] temp = new Vector3[mesh_triangles_indices.Length];
        for(int i = 0 ; i < mesh_triangles_indices.Length/3 ; i++) {
            temp[3*i] = mesh_vertices[mesh_triangles_indices[3*i]];
            temp[3*i+1] = mesh_vertices[mesh_triangles_indices[3*i+1]];
            temp[3*i+2] = mesh_vertices[mesh_triangles_indices[3*i+2]];
        }
        mesh_vertices = temp;

    }

    void Compute_Mesh_State() {
        mesh_state = new int[mesh_vertices.Length];
        for(int i = 0 ; i < mesh_vertices.Length ; i++) {
            Vector3 v = mesh_vertices[i];
            if(Solve(v) > 0)
                mesh_state[i] = 1;
            else if(Solve(v) < 0)
                mesh_state[i] = 0;
            else mesh_state[i] = -1;
        }
    }


    void Compute_New_Triangles() {
        for(int i = 0 ; i < mesh_vertices.Length/3 ; i++) {
            Vector3[] v = new Vector3[3] {mesh_vertices[3*i], mesh_vertices[3*i+1], mesh_vertices[3*i+2]};
            int[] s = new int[3] {mesh_state[3*i], mesh_state[3*i+1], mesh_state[3*i+2]};
            Compute_One_Triangle(v, s);
        }
    }



    void Compute_One_Triangle(Vector3[] v, int[] s) {

        
        if(s[0] == 1 && s[1] == 1 && s[2] == 1) {
            right_mesh_vertices.AddRange(v);
            Add_Triangle_Indices_Right(0,1,2);
        }
        else if(s[0] == 0 && s[1] == 0 && s[2] == 0) {
            left_mesh_vertices.AddRange(v);
            Add_Triangle_Indices_Left(0,1,2);
        }
        
        else if(s[0] == 1 && s[1] == 1 && s[2] == 0) {                 
            Add_Triangle_Vertices_Left(v[2], Inter(v[0], v[2]), Inter(v[1], v[2]));
            Add_Triangle_Vertices_Right(v[1], Inter(v[1], v[2]), Inter(v[0], v[2]));
            Add_Triangle_Vertices_Right(v[0], v[1], Inter(v[0], v[2]));

            new_vertices.Add(Inter(v[0], v[2]));
            new_vertices.Add(Inter(v[1], v[2]));

            Add_Triangle_Indices_Left(0,1,2);
            Add_Triangle_Indices_Right(0,1,2);
            Add_Triangle_Indices_Right(0,1,2);
        }
        else if(s[0] == 1 && s[1] == 0 && s[2] == 1) {          
            Add_Triangle_Vertices_Left(v[1], Inter(v[1], v[2]), Inter(v[0], v[1]));
            Add_Triangle_Vertices_Right(v[0], Inter(v[0], v[1]), Inter(v[1], v[2]));
            Add_Triangle_Vertices_Right(Inter(v[1], v[2]), v[2], v[0]);

            new_vertices.Add(Inter(v[0], v[1]));
            new_vertices.Add(Inter(v[1], v[2]));

            Add_Triangle_Indices_Left(0,1,2);
            Add_Triangle_Indices_Right(0,1,2);
            Add_Triangle_Indices_Right(0,1,2); 
        } 
        else if(s[0] == 0 && s[1] == 1 && s[2] == 1) {              
            Add_Triangle_Vertices_Left(v[0], Inter(v[0], v[1]), Inter(v[0], v[2]));
            Add_Triangle_Vertices_Right(v[1], Inter(v[0], v[2]), Inter(v[0], v[1]));
            Add_Triangle_Vertices_Right(v[1], v[2], Inter(v[0], v[2]));

            new_vertices.Add(Inter(v[0], v[1]));
            new_vertices.Add(Inter(v[0], v[2]));

            Add_Triangle_Indices_Left(0,1,2);
            Add_Triangle_Indices_Right(0,1,2);
            Add_Triangle_Indices_Right(0,1,2);
        }



        if(s[0] == 1 && s[1] == 0 && s[2] == 0) {           
            Add_Triangle_Vertices_Right(Inter(v[0], v[1]), Inter(v[0], v[2]), v[0]);
            Add_Triangle_Vertices_Left(Inter(v[0], v[2]), Inter(v[0], v[1]), v[1]);        
            Add_Triangle_Vertices_Left(v[1], v[2], Inter(v[0], v[2]));

            new_vertices.Add(Inter(v[0], v[1]));
            new_vertices.Add(Inter(v[0], v[2]));

            Add_Triangle_Indices_Right(0,1,2);
            Add_Triangle_Indices_Left(0,1,2);
            Add_Triangle_Indices_Left(0,1,2);
        } 
        else if(s[0] == 0 && s[1] == 1 && s[2] == 0) {         
            Add_Triangle_Vertices_Right(v[1], Inter(v[1], v[2]), Inter(v[0], v[1]));
            Add_Triangle_Vertices_Left(v[0], Inter(v[0], v[1]), Inter(v[1], v[2]));
            Add_Triangle_Vertices_Left(Inter(v[1], v[2]), v[2], v[0]);

            new_vertices.Add(Inter(v[0], v[1]));
            new_vertices.Add(Inter(v[1], v[2]));

            Add_Triangle_Indices_Right(0,1,2);  
            Add_Triangle_Indices_Left(0,1,2); 
            Add_Triangle_Indices_Left(0,1,2);    
        } 
        else if(s[0] == 0 && s[1] == 0 && s[2] == 1) {
            Add_Triangle_Vertices_Right(v[2], Inter(v[0], v[2]), Inter(v[1], v[2]));
            Add_Triangle_Vertices_Left(v[1], Inter(v[1], v[2]), Inter(v[0], v[2]));
            Add_Triangle_Vertices_Left(v[0], v[1], Inter(v[0], v[2]));

            new_vertices.Add(Inter(v[0], v[2])); 
            new_vertices.Add(Inter(v[1], v[2]));

            Add_Triangle_Indices_Right(0,1,2);
            Add_Triangle_Indices_Left(0,1,2);
            Add_Triangle_Indices_Left(0,1,2);
        }


    }


    void Inset_Faces() {

        centroid = Vector3.zero;
        foreach(Vector3 v in new_vertices)
            centroid += v;
        centroid /= new_vertices.Count;


        //Remove_Duplicate_Inside_Face_Vertices();
        Sort_Inside_Vertices(centroid);
        //Remove_Congruent_Vertices();


        for(int i = 0 ; i < new_vertices.Count-1 ; i++) {

            Add_Triangle_Vertices_Right(centroid, new_vertices[i], new_vertices[i+1]);
            Add_Triangle_Vertices_Left(centroid, new_vertices[i], new_vertices[i+1]);

            Add_Triangle_Indices_Right(2,1,0);
            Add_Triangle_Indices_Left(0,1,2);

        }

        Add_Triangle_Vertices_Right(centroid, new_vertices[new_vertices.Count-1], new_vertices[0]);
        Add_Triangle_Vertices_Left(centroid, new_vertices[new_vertices.Count-1], new_vertices[0]);

        Add_Triangle_Indices_Right(2,1,0);
        Add_Triangle_Indices_Left(0,1,2);

    }



    void Remove_Duplicate_Inside_Face_Vertices() {

        for(int i = 0 ; i < new_vertices.Count ; i++)
            for(int j = 0 ; j < new_vertices.Count ; j++)
                if(new_vertices[i] == new_vertices[j] && i != j) {
                    new_vertices.RemoveAt(i);
                    i--;
                    break;
                }

    }



    void Sort_Inside_Vertices(Vector3 centroid) {

        List<float> angle_list = new List<float>();
        List<Vector3> sorted_vertices = new List<Vector3>();
        float[] sorting_plane = Compute_Plane(Vector3.Cross(centroid-mesh_vertices[0], slicer.right), new_vertices[0]);

        for(int i = 0 ; i < new_vertices.Count ; i++) {
            angle_list.Add(Vector3.SignedAngle(new_vertices[i],centroid-mesh_vertices[0], slicer.right));
            if(sorting_plane[0]*new_vertices[i].x + sorting_plane[1]*new_vertices[i].y + sorting_plane[2]*new_vertices[i].z + sorting_plane[3] < 0)
            angle_list[i] = 180-angle_list[i];
        }

        while(angle_list.Count > 0) {
            float min_angle = float.MaxValue;
            int index = 0;
            for(int i = 0 ; i < angle_list.Count ; i++) 
                if(angle_list[i] < min_angle) {
                    min_angle = angle_list[i];
                    index = i;
                }
            sorted_vertices.Add(new_vertices[index]);
            new_vertices.RemoveAt(index);
            angle_list.RemoveAt(index);
        }

        new_vertices = sorted_vertices;

    }



    void Remove_Congruent_Vertices() {

        int c = new_vertices.Count;
        for(int i = 0 ; i < c-1 ; i++) {
            if(Mathf.Abs(Vector3.Dot((new_vertices[(i+1)%c] - new_vertices[i]).normalized, (new_vertices[(i+2)%c] - new_vertices[i]).normalized)) == 1) {
                new_vertices.Remove(new_vertices[(i+1)%c]);
                i--;
            }
        }

    }



    void Create_New_Objects() {

        new_right_gameObject = Create_New_Object(right_mesh_vertices, right_mesh_triangle_indices);
        new_left_gameObject = Create_New_Object(left_mesh_vertices, left_mesh_triangle_indices);

    }

    GameObject Create_New_Object(List<Vector3> vertices, List<int> triangles) {

        Mesh new_mesh = new Mesh();
        for(int i = 0 ; i < vertices.Count ; i++) {
            Vector3 v = vertices[i];
            v = v - object_To_Slice.transform.position;
            v = Quaternion.Inverse(object_To_Slice.transform.rotation) * v;
            v = new Vector3(v.x / object_To_Slice.transform.localScale.x, v.y / object_To_Slice.transform.localScale.y, v.z / object_To_Slice.transform.localScale.z);
            vertices[i] = new Vector3(v.x, v.y, v.z);
        }

        new_mesh.vertices = vertices.ToArray();
        new_mesh.triangles = triangles.ToArray();

        GameObject new_object = Instantiate(object_To_Slice, object_To_Slice.transform.position, object_To_Slice.transform.rotation);
        new_object.GetComponent<MeshFilter>().mesh = new_mesh;
        new_object.GetComponent<MeshFilter>().mesh.RecalculateNormals();

        new_object.name = object_To_Slice.name + " copy";

        if(new_object.GetComponent<Collider>() && !new_object.GetComponent<MeshCollider>()) {
            Destroy(new_object.GetComponent<Collider>());
            new_object.AddComponent<MeshCollider>();
        }
        new_object.GetComponent<MeshCollider>().sharedMesh = new_mesh;
        if(new_object.GetComponent<Rigidbody>()) {
            new_object.GetComponent<MeshCollider>().convex = true;
            new_object.GetComponent<Rigidbody>().velocity = object_To_Slice.GetComponent<Rigidbody>().velocity;
            new_object.GetComponent<Rigidbody>().centerOfMass = Compute_Center_Of_Mass(vertices);
        }

        new_object.transform.parent = parent;

        new_object.layer = 0;

        return new_object;

    }



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


    Vector3 Compute_Center_Of_Mass(List<Vector3> vertices) {
        Vector3 com = Vector3.zero;
        foreach(Vector3 v in vertices) {
            com += v;
        }
        com /= vertices.Count;
        return com;
    }


    void Add_Triangle_Vertices_Left(Vector3 v1, Vector3 v2, Vector3 v3) {
        left_mesh_vertices.Add(v1);
        left_mesh_vertices.Add(v2);
        left_mesh_vertices.Add(v3);
    }
    void Add_Triangle_Vertices_Right(Vector3 v1, Vector3 v2, Vector3 v3) {
        right_mesh_vertices.Add(v1);
        right_mesh_vertices.Add(v2);
        right_mesh_vertices.Add(v3);
    }
    void Add_Triangle_Indices_Left(int i1, int i2, int i3) {
        left_mesh_triangle_indices.Add(i1 + left_indices_offset);
        left_mesh_triangle_indices.Add(i2 + left_indices_offset);
        left_mesh_triangle_indices.Add(i3 + left_indices_offset);
        left_indices_offset+=3;
    }
    void Add_Triangle_Indices_Right(int i1, int i2, int i3) {
        right_mesh_triangle_indices.Add(i1 + right_indices_offset);
        right_mesh_triangle_indices.Add(i2 + right_indices_offset);
        right_mesh_triangle_indices.Add(i3 + right_indices_offset);
        right_indices_offset+=3;
    }



}
