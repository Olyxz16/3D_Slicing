using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;    


public class SlicerScript : MonoBehaviour
{    
    
    
    GameObject object_To_Slice;
    Transform parent;
    Mesh mesh;
    Vector3[] mesh_vertices;
    int[] mesh_triangles_indice;
    int[] mesh_state;

    float[] plane_values;
    Vector3 normal, point;

    List<Vector3> right_mesh_vertices, left_mesh_vertices;
    List<Vector3> new_vertices;
    List<int> right_mesh_triangle_indice, left_mesh_triangle_indice;
    List<int> new_right_triangle_indice, new_left_triangle_indice;
    int right_indice_offset, left_indice_offset, new_right_indice_offset, new_left_indice_offset;

    Vector3 right_center_of_mass, left_center_of_mass;

    Mesh new_right_mesh, new_left_mesh;
    GameObject new_left_gameObject, new_right_gameObject;



    #region Slice Functions

    
    public GameObject[] Slice(GameObject obj, Vector3 normal, Vector3 point, int new_layer = -1) {

        float t = Time.realtimeSinceStartup;
        int n = obj.GetComponent<MeshFilter>().mesh.vertices.Length;

        Initialize_Mesh(obj, normal, point);
        Compute_Plane();
        Compute_Mesh_Vertices();
        Compute_Mesh_State();

        Compute_New_Triangles();
        Inset_Faces();
        Create_New_Objects(new_layer);

        Destroy(object_To_Slice);

        Debug.Log(new_left_gameObject.transform.position);
        Debug.Log(new_right_gameObject.transform.position);

        return new GameObject[] {new_left_gameObject, new_right_gameObject};

        //float endTime = Time.realtimeSinceStartup;
        //Debug.Log(endTime - t + " " + n);

    }

    public GameObject[] Slice(GameObject obj, Transform sli = default, int new_layer = -1) {
        if(sli == default)
            sli = this.transform;
        return Slice(obj, sli.right, sli.position, new_layer);
    }


    public GameObject[] Slice(GameObject obj, Material mat, int new_layer = -1) {

        Initialize_Mesh(obj, this.transform.right, this.transform.position);
        Compute_Plane();
        Compute_Mesh_Vertices();
        Compute_Mesh_State();
        Compute_New_Triangles();
        Inset_Faces();
        Create_New_Objects(mat, new_layer);
        Destroy(object_To_Slice);

        return new GameObject[] {new_left_gameObject, new_right_gameObject};

    }

    #endregion


    #region Initialization

    void Initialize_Mesh (GameObject obj, Vector3 norm, Vector3 pt) {  

        object_To_Slice = obj;
        parent = object_To_Slice.transform.parent;
        mesh = obj.GetComponent<MeshFilter>().mesh;

        normal = norm;
        point = pt;

        mesh_vertices = mesh.vertices;
        for(int i = 0 ; i < mesh_vertices.Length ; i++) {
            Vector3 v = mesh_vertices[i];
            v = new Vector3(v.x * object_To_Slice.transform.localScale.x, v.y * object_To_Slice.transform.localScale.y, v.z * object_To_Slice.transform.localScale.z);
            v = object_To_Slice.transform.rotation * v;
            v = v + object_To_Slice.transform.position;
            mesh_vertices[i] = new Vector3(v.x, v.y, v.z);
        }
           
        mesh_triangles_indice = mesh.triangles;

        right_mesh_vertices = new List<Vector3>();
        left_mesh_vertices = new List<Vector3>();
        new_vertices = new List<Vector3>();

        right_mesh_triangle_indice = new List<int>();
        left_mesh_triangle_indice = new List<int>();
        new_right_triangle_indice = new List<int>();
        new_left_triangle_indice = new List<int>();

        right_indice_offset = 0;
        left_indice_offset = 0;
        new_right_indice_offset = 0;
        new_left_indice_offset = 0;

    }


    void Compute_Plane() {
        plane_values = new float[4] {
            -normal.x,
            -normal.y,
            -normal.z,
            normal.x*point.x + normal.y*point.y + normal.z*point.z
        };
    }


    void Compute_Mesh_Vertices() {

        Vector3[] temp = new Vector3[mesh_triangles_indice.Length];
        for(int i = 0 ; i < mesh_triangles_indice.Length/3 ; i++) {
            temp[3*i] = mesh_vertices[mesh_triangles_indice[3*i]];
            temp[3*i+1] = mesh_vertices[mesh_triangles_indice[3*i+1]];
            temp[3*i+2] = mesh_vertices[mesh_triangles_indice[3*i+2]];
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

    #endregion


    #region Triangle_Creation

    void Compute_New_Triangles() {
        for(int i = 0 ; i < mesh_vertices.Length/3 ; i++) {
            Vector3[] v = new Vector3[3] {mesh_vertices[3*i], mesh_vertices[3*i+1], mesh_vertices[3*i+2]};
            int[] s = new int[3] {mesh_state[3*i], mesh_state[3*i+1], mesh_state[3*i+2]};
            Compute_Triangle(v, s);
        }
    }



    void Compute_Triangle(Vector3[] v, int[] s) {

        
        if(s[0] == 1 && s[1] == 1 && s[2] == 1) {
            right_mesh_vertices.AddRange(v);
            Add_Triangle_Indice_Right(0,1,2);
        }
        else if(s[0] == 0 && s[1] == 0 && s[2] == 0) {
            left_mesh_vertices.AddRange(v);
            Add_Triangle_Indice_Left(0,1,2);
        }
        
        else if(s[0] == 1 && s[1] == 1 && s[2] == 0) {                 
            Add_Triangle_Vertices_Left(v[2], Inter(v[0], v[2]), Inter(v[1], v[2]));
            Add_Triangle_Vertices_Right(v[1], Inter(v[1], v[2]), Inter(v[0], v[2]));
            Add_Triangle_Vertices_Right(v[0], v[1], Inter(v[0], v[2]));

            new_vertices.Add(Inter(v[0], v[2]));
            new_vertices.Add(Inter(v[1], v[2]));

            Add_Triangle_Indice_Left(0,1,2);
            Add_Triangle_Indice_Right(0,1,2);
            Add_Triangle_Indice_Right(0,1,2);
        }
        else if(s[0] == 1 && s[1] == 0 && s[2] == 1) {          
            Add_Triangle_Vertices_Left(v[1], Inter(v[1], v[2]), Inter(v[0], v[1]));
            Add_Triangle_Vertices_Right(v[0], Inter(v[0], v[1]), Inter(v[1], v[2]));
            Add_Triangle_Vertices_Right(Inter(v[1], v[2]), v[2], v[0]);

            new_vertices.Add(Inter(v[0], v[1]));
            new_vertices.Add(Inter(v[1], v[2]));

            Add_Triangle_Indice_Left(0,1,2);
            Add_Triangle_Indice_Right(0,1,2);
            Add_Triangle_Indice_Right(0,1,2); 
        } 
        else if(s[0] == 0 && s[1] == 1 && s[2] == 1) {              
            Add_Triangle_Vertices_Left(v[0], Inter(v[0], v[1]), Inter(v[0], v[2]));
            Add_Triangle_Vertices_Right(v[1], Inter(v[0], v[2]), Inter(v[0], v[1]));
            Add_Triangle_Vertices_Right(v[1], v[2], Inter(v[0], v[2]));

            new_vertices.Add(Inter(v[0], v[1]));
            new_vertices.Add(Inter(v[0], v[2]));

            Add_Triangle_Indice_Left(0,1,2);
            Add_Triangle_Indice_Right(0,1,2);
            Add_Triangle_Indice_Right(0,1,2);
        }



        if(s[0] == 1 && s[1] == 0 && s[2] == 0) {           
            Add_Triangle_Vertices_Right(Inter(v[0], v[1]), Inter(v[0], v[2]), v[0]);
            Add_Triangle_Vertices_Left(Inter(v[0], v[2]), Inter(v[0], v[1]), v[1]);        
            Add_Triangle_Vertices_Left(v[1], v[2], Inter(v[0], v[2]));

            new_vertices.Add(Inter(v[0], v[1]));
            new_vertices.Add(Inter(v[0], v[2]));

            Add_Triangle_Indice_Right(0,1,2);
            Add_Triangle_Indice_Left(0,1,2);
            Add_Triangle_Indice_Left(0,1,2);
        } 
        else if(s[0] == 0 && s[1] == 1 && s[2] == 0) {         
            Add_Triangle_Vertices_Right(v[1], Inter(v[1], v[2]), Inter(v[0], v[1]));
            Add_Triangle_Vertices_Left(v[0], Inter(v[0], v[1]), Inter(v[1], v[2]));
            Add_Triangle_Vertices_Left(Inter(v[1], v[2]), v[2], v[0]);

            new_vertices.Add(Inter(v[0], v[1]));
            new_vertices.Add(Inter(v[1], v[2]));

            Add_Triangle_Indice_Right(0,1,2);  
            Add_Triangle_Indice_Left(0,1,2); 
            Add_Triangle_Indice_Left(0,1,2);    
        } 
        else if(s[0] == 0 && s[1] == 0 && s[2] == 1) {
            Add_Triangle_Vertices_Right(v[2], Inter(v[0], v[2]), Inter(v[1], v[2]));
            Add_Triangle_Vertices_Left(v[1], Inter(v[1], v[2]), Inter(v[0], v[2]));
            Add_Triangle_Vertices_Left(v[0], v[1], Inter(v[0], v[2]));

            new_vertices.Add(Inter(v[0], v[2])); 
            new_vertices.Add(Inter(v[1], v[2]));

            Add_Triangle_Indice_Right(0,1,2);
            Add_Triangle_Indice_Left(0,1,2);
            Add_Triangle_Indice_Left(0,1,2);
        }


    }

    #endregion


    #region Face_Inset

    void Inset_Faces() {

        Vector3 centroid = Vector3.zero;
        foreach(Vector3 v in new_vertices)
            centroid += v;
        centroid /= new_vertices.Count;


        Remove_Duplicate_Inside_Face_Vertices();
        Sort_Inside_Vertices(centroid);
        Remove_Congruent_Vertices();

        Create_Face_Triangles(centroid);

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

        Quaternion rot = Quaternion.FromToRotation(normal, Vector3.back);


        for(int i = 0 ; i < new_vertices.Count ; i++) {
            Vector3 dir = new_vertices[i] - centroid;
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
            sorted_vertices.Add(new_vertices[index]);
            new_vertices.RemoveAt(index);
            angle_list.RemoveAt(index);
            indice--;
        }

        new_vertices = sorted_vertices;

    }



    void Remove_Congruent_Vertices() {

        int c = new_vertices.Count;
        for(int i = c-1 ; i > 1 ; i--) {
            if(Mathf.Abs(Vector3.Dot((new_vertices[(i-1)%c] - new_vertices[i%c]).normalized, (new_vertices[(i-2)%c] - new_vertices[i%c]).normalized)) == 1) {
                new_vertices.RemoveAt((i-1)%c);
                c--;
                i++;
            }
        }

    }


    void Create_Face_Triangles(Vector3 centroid) {

        for(int i = 0 ; i < new_vertices.Count-1 ; i++) {

            Add_Triangle_Vertices_Right(centroid, new_vertices[i], new_vertices[i+1]);
            Add_Triangle_Vertices_Left(centroid, new_vertices[i], new_vertices[i+1]);

            Add_Triangle_Indice_Right(2,1,0);
            Add_Triangle_Indice_Left(0,1,2);

        }

        Add_Triangle_Vertices_Right(centroid, new_vertices[new_vertices.Count-1], new_vertices[0]);
        Add_Triangle_Vertices_Left(centroid, new_vertices[new_vertices.Count-1], new_vertices[0]);

        Add_Triangle_Indice_Right(2,1,0);
        Add_Triangle_Indice_Left(0,1,2);

    }


    #endregion


    #region New_Object_Creation

    void Create_New_Objects(int layer) {
            new_right_gameObject = Create_New_Object(right_mesh_vertices, right_mesh_triangle_indice, new_right_triangle_indice, layer);
            new_left_gameObject = Create_New_Object(left_mesh_vertices, left_mesh_triangle_indice, new_left_triangle_indice, layer);
    }
    void Create_New_Objects(Material mat, int layer) {
            new_right_gameObject = Create_New_Object(right_mesh_vertices, right_mesh_triangle_indice, new_right_triangle_indice, mat, layer);
            new_left_gameObject = Create_New_Object(left_mesh_vertices, left_mesh_triangle_indice, new_left_triangle_indice, mat, layer);
    }

    GameObject Create_New_Object(List<Vector3> vertices, List<int> triangles, List<int> new_triangles, int layer) {

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
            new_object.GetComponent<Rigidbody>().angularVelocity = object_To_Slice.GetComponent<Rigidbody>().angularVelocity;
            new_object.GetComponent<Rigidbody>().centerOfMass = Vector3.zero;
            //new_object.GetComponent<Rigidbody>().ResetCenterOfMass();
        }

        if(layer == -1) 
            new_object.layer = object_To_Slice.layer;
        else
            new_object.layer = layer;
        new_object.transform.parent = parent;

        return new_object;

    }
    

    GameObject Create_New_Object(List<Vector3> vertices, List<int> triangles, List<int> new_triangles, Material mat, int layer) {

        Mesh new_mesh = new Mesh();
        Mesh old_mesh = object_To_Slice.GetComponent<MeshFilter>().mesh; 
        for(int i = 0 ; i < vertices.Count ; i++) {
            Vector3 v = vertices[i];
            v = v - object_To_Slice.transform.position;
            v = Quaternion.Inverse(object_To_Slice.transform.rotation) * v;
            v = new Vector3(v.x / object_To_Slice.transform.localScale.x, v.y / object_To_Slice.transform.localScale.y, v.z / object_To_Slice.transform.localScale.z);
            vertices[i] = new Vector3(v.x, v.y, v.z);
        }


        bool is_material_part_of_object = false;
        int index = -1;
        Material[] m = object_To_Slice.GetComponent<MeshRenderer>().materials;
        for(int i = 0 ; i < object_To_Slice.GetComponent<MeshRenderer>().materials.Length ; i++) {
            if(mat == m[i]) {
                is_material_part_of_object = true;
                index = i;
                break;
            }  
        }

        int submesh_count;
        if(is_material_part_of_object)
            submesh_count = object_To_Slice.GetComponent<MeshRenderer>().materials.Length;
        else submesh_count = object_To_Slice.GetComponent<MeshRenderer>().materials.Length + 1;

        

        Material[] mats = new Material[submesh_count];
        if(is_material_part_of_object) {
            mats = object_To_Slice.GetComponent<MeshRenderer>().materials;
        }
        else {
            for(int i = 0 ; i < submesh_count-1 ; i++) 
                mats[i] = object_To_Slice.GetComponent<MeshRenderer>().materials[i];
            mats[submesh_count-1] = mat;
        }
        
        

        new_mesh.subMeshCount = submesh_count;
        new_mesh.SetVertices(vertices);
        for(int i = 0 ; i < submesh_count-1 ; i++) {
            if(index == i) {
                new_mesh.SetTriangles(new_triangles, i);
                continue;
            }
            new_mesh.SetTriangles(old_mesh.GetTriangles(i), i);
        }

        GameObject new_object = Instantiate(object_To_Slice, object_To_Slice.transform.position, object_To_Slice.transform.rotation);
        new_object.GetComponent<MeshFilter>().mesh.Clear();
        new_object.GetComponent<MeshFilter>().mesh = new_mesh;
        new_object.GetComponent<MeshRenderer>().materials = mats;

        new_object.name = object_To_Slice.name + " copy";

        if(new_object.GetComponent<Collider>() && !new_object.GetComponent<MeshCollider>()) {
            Destroy(new_object.GetComponent<Collider>());
            new_object.AddComponent<MeshCollider>();
        }
        new_object.GetComponent<MeshCollider>().sharedMesh = new_mesh;
        if(new_object.GetComponent<Rigidbody>()) {
            new_object.GetComponent<MeshCollider>().convex = true;
            new_object.GetComponent<Rigidbody>().velocity = object_To_Slice.GetComponent<Rigidbody>().velocity;
            new_object.GetComponent<Rigidbody>().angularVelocity = object_To_Slice.GetComponent<Rigidbody>().angularVelocity;
            Rigidbody rb = new_object.GetComponent<Rigidbody>();
            new_object.GetComponent<Rigidbody>().centerOfMass = Vector3.zero;
        }

        if(layer == -1) 
            new_object.layer = object_To_Slice.layer;
        else
            new_object.layer = layer;
        new_object.transform.parent = parent;

        return new_object;

    }
    

    #endregion


    #region tools

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


    Vector3 Compute_Centroid(Vector3[] vertices) {
        Vector3 com = Vector3.zero;
        foreach(Vector3 v in vertices) {
            com += v;
        }
        com /= vertices.Length;
        return com;
    }

    Vector3 Compute_Center_Of_Mass(Vector3[] vertices) {

        Vector3 min_values = vertices[0];
        Vector3 max_values = vertices[0];
        foreach(Vector3 v in vertices) {

            if(v.x < min_values.x)
                min_values.x = v.x;
            if(v.y < min_values.y)
                min_values.y = v.y;
            if(v.z < min_values.z)
                min_values.z = v.z;
            
            if(v.x > max_values.x)
                max_values.x = v.x;
            if(v.y > max_values.y)
                max_values.y = v.y;
            if(v.z > max_values.z)
                max_values.z = v.z;

        }

        Vector3 com = new Vector3((min_values.x + max_values.x)/2, (min_values.y + max_values.y)/2, (min_values.z + max_values.z)/2);
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
    void Add_Triangle_Indice_Left(int i1, int i2, int i3) {
        left_mesh_triangle_indice.Add(i1 + left_indice_offset);
        left_mesh_triangle_indice.Add(i2 + left_indice_offset);
        left_mesh_triangle_indice.Add(i3 + left_indice_offset);
        left_indice_offset+=3;
    }
    void Add_Triangle_Indice_Right(int i1, int i2, int i3) {
        right_mesh_triangle_indice.Add(i1 + right_indice_offset);
        right_mesh_triangle_indice.Add(i2 + right_indice_offset);
        right_mesh_triangle_indice.Add(i3 + right_indice_offset);
        right_indice_offset+=3;
    }


    void Add_New_Triangles_Indice_Right(int i1, int i2, int i3) {
        new_right_triangle_indice.Add(i1 + new_right_indice_offset);
        new_right_triangle_indice.Add(i2 + new_right_indice_offset);
        new_right_triangle_indice.Add(i3 + new_right_indice_offset);
        new_right_indice_offset+=3;
    }
    void Add_New_Triangles_Indice_Left(int i1, int i2, int i3) {
        new_left_triangle_indice.Add(i1 + new_left_indice_offset);
        new_left_triangle_indice.Add(i2 + new_left_indice_offset);
        new_left_triangle_indice.Add(i3 + new_left_indice_offset);
        new_left_indice_offset+=3;
    }


    #endregion

}
