/// <summary>
/// CodeArtist.mx 2015
/// This is the main class of the project, its in charge of raycasting to a model and place brush prefabs infront of the canvas camera.
/// If you are interested in saving the painted texture you can use the method at the end and should save it to a file.
/// </summary>


using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public enum Painter_BrushMode{PAINT,DECAL};

public enum GamePhase
{
	ClearRust,
	Painting,
	Finished
}

public class TexturePainter : MonoBehaviour {

	public int currentLevelIndex = 0;
	public GamePhase gamePhase;

	public TextMeshProUGUI txtPercent;
	public Color maskColor;
	public Color cleanColor;
	public Color paintedColor;

	public int cleanPercent = 95;
	public int paintedPercent = 95;
	public float toolRotationSpeed = 2.0f;
	public float toolMovementSpeed = 2.0f;

	public Texture2D paintedMaskTexture;
	public Texture rustTexture;
	public GameObject goSponge;
	public Spray spray;
	public GameObject goPaintObject;
	public GameObject brushCursor,brushContainer; //The cursor that overlaps the model and our container for the brushes painted
	public Camera sceneCamera,canvasCam;  //The camera that looks at the model, and the camera that looks at the canvas.
	public Sprite cursorPaint,cursorDecal; // Cursor for the differen functions 
	public RenderTexture canvasTexture; // Render Texture that looks at our Base Texture and the painted brushes
	public Material baseMaterial; // The material of our base texture (Were we will save the painted texture)

	public GameObject endGameUI;
	public Button btnRestartLevel;
	public Button btnNextLevel;

	public Transform cameraPivot;
	Vector3 cameraPivotPos;
	Quaternion cameraPivotRot;
	public Transform mainCamera;
	Vector3 cameraPos;
	Quaternion cameraRot;

	Painter_BrushMode mode; //Our painter mode (Paint brushes or decals)
	float brushSize=1.5f; //The size of our brush
	Color brushColor; //The selected color
	int brushCounter=0,MAX_BRUSH_COUNT=1000; //To avoid having millions of brushes
	bool saving=false; //Flag to check if we are saving the texture
	int maskPixelCount = 0;
	int paintedPixelCount = 0;
	List<Vector2> maskPositionsBackup = new List<Vector2>();
	List<Vector2> maskPositions = new List<Vector2>();
	List<Vector2> sprayMaskPositions = new List<Vector2>();
	Texture2D cleanTexture;
	Texture2D paintedTexture;
	bool needToCheckPaintedPercent = false;

	void Start()
    {
		endGameUI.SetActive(false);
		gamePhase = GamePhase.ClearRust;
		baseMaterial.mainTexture = rustTexture;
		brushColor = cleanColor;
		spray.Hide();

		ParseMaskPositions();
		CreateCompletedTextures();
		SetClickListeners();
		BackupCameraData();
	}

	// this mask logic will help us to detect percantage of painted area of object
	// also we dont need to check every pixel at texture with this logic
	void ParseMaskPositions()
    {
		for(int i = 0; i < paintedMaskTexture.width; i++)
        {
			for(int j = 0; j < paintedMaskTexture.height; j++)
            {
				Color currentColor = paintedMaskTexture.GetPixel(i, j);
				if(currentColor != Color.white)
                {
					if (currentColor == maskColor)
                    {
						Vector2 pos = new Vector2(i, j);
						maskPositionsBackup.Add(pos);
						maskPositions.Add(pos);
						sprayMaskPositions.Add(pos);
					}
				}
			}
        }

		maskPixelCount = maskPositions.Count;
	}

	// this method will help us preventing frame drop when swaping gamePhase
	void CreateCompletedTextures()
    {
		cleanTexture = new Texture2D(canvasTexture.width, canvasTexture.height, TextureFormat.RGBA32, false);
		for (int i = 0; i < canvasTexture.width; i++)
		{
			for (int j = 0; j < canvasTexture.height; j++)
				cleanTexture.SetPixel(i, j, cleanColor);
		}
		cleanTexture.Apply();

		paintedTexture = new Texture2D(canvasTexture.width, canvasTexture.height, TextureFormat.RGBA32, false);
		for (int i = 0; i < canvasTexture.width; i++)
		{
			for (int j = 0; j < canvasTexture.height; j++)
				paintedTexture.SetPixel(i, j, paintedColor);
		}
		paintedTexture.Apply();
	}

	void SetClickListeners()
    {
		btnRestartLevel.onClick.AddListener(() => RestartLevel());
		btnNextLevel.onClick.AddListener(() => NextLevel());
    }

	void BackupCameraData()
    {
		cameraPivotPos = cameraPivot.position;
		cameraPivotRot = cameraPivot.rotation;

		cameraPos = mainCamera.position;
		cameraRot = mainCamera.rotation;
    }

	void RestoreCameraData()
    {
		cameraPivot.position = cameraPivotPos;
		cameraPivot.rotation = cameraPivotRot;

		mainCamera.position = cameraPos;
		mainCamera.rotation = cameraRot;
    }

	void RestartLevel()
    {
		Debug.Log("RestartLevel");
		SceneManager.LoadScene(currentLevelIndex);
    }

	void NextLevel()
    {
		//SceneManager.LoadScene(currentLevelIndex + 1); // TODO
	}

	void Update ()
	{
		if (gamePhase == GamePhase.Finished)
			return;

		//brushColor = ColorSelector.GetColor();	//Updates our painted color with the selected color
		if (Input.GetMouseButton(0))
			DoAction();

		//UpdateBrushCursor();

		RaycastHit hit;
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out hit))
		{
			if(gamePhase == GamePhase.ClearRust)
            {
				goSponge.transform.position = Vector3.Slerp(goSponge.transform.position, hit.point, Time.deltaTime * toolMovementSpeed);
				Quaternion qTemp = Quaternion.FromToRotation(goSponge.transform.forward, hit.normal * 2) * goSponge.transform.rotation;
				Vector3 tempRot = qTemp.eulerAngles;
				tempRot.z = 0.0f;
				goSponge.transform.rotation = Quaternion.Slerp(goSponge.transform.rotation, Quaternion.Euler(tempRot), Time.deltaTime * toolRotationSpeed);

			}
			else if(gamePhase == GamePhase.Painting)
            {
				spray.transform.position = Vector3.Slerp(spray.transform.position, hit.normal * 2 + hit.point, Time.deltaTime * toolMovementSpeed);
				Quaternion qTemp = Quaternion.FromToRotation(spray.transform.forward, hit.normal * 2) * spray.transform.rotation;
				Vector3 tempRot = qTemp.eulerAngles;
				tempRot.z = 0.0f;
				spray.transform.rotation = Quaternion.Slerp(spray.transform.rotation, Quaternion.Euler(tempRot), Time.deltaTime * toolRotationSpeed);
			}
		}else
        {
			if(gamePhase == GamePhase.ClearRust)
            {
				Vector3 temp = Input.mousePosition;
				temp.z = 10f;
				goSponge.transform.position = Vector3.Slerp(goSponge.transform.position, Camera.main.ScreenToWorldPoint(temp), Time.deltaTime * toolMovementSpeed);
				goSponge.transform.rotation = Quaternion.Slerp(goSponge.transform.rotation, Quaternion.Euler(new Vector3(0.0f, 180.0f, 0.0f)), Time.deltaTime * toolRotationSpeed);
			}
			else if(gamePhase == GamePhase.Painting)
            {
				Vector3 temp = Input.mousePosition;
				temp.z = 10f;
				spray.transform.position = Vector3.Slerp(spray.transform.position, Camera.main.ScreenToWorldPoint(temp), Time.deltaTime * toolMovementSpeed);
				spray.transform.rotation = Quaternion.Slerp(spray.transform.rotation, Quaternion.Euler(new Vector3(0.0f, 180.0f, 0.0f)), Time.deltaTime * toolRotationSpeed);
			}
		}

		// left mouse button down
		if (Input.GetMouseButtonDown(0))
        {
			needToCheckPaintedPercent = true;
			if (gamePhase == GamePhase.Painting)
				spray.StartSpray();
		}

		if (Input.GetMouseButton(0))
			needToCheckPaintedPercent = true;

		// release left mouse button
		if (Input.GetMouseButtonUp(0))
        {
			needToCheckPaintedPercent = true;
			if (gamePhase == GamePhase.Painting)
				spray.StopSpray();
		}
	}

    void FixedUpdate()
    {
		if (gamePhase != GamePhase.Finished)
			StartCoroutine(CheckPaintedPercent());
	}

	public IEnumerator CheckPaintedPercent()
	{
		yield return new WaitForEndOfFrame();

		if (needToCheckPaintedPercent)
		{
			RenderTexture.active = canvasTexture;
			Texture2D tex = new Texture2D(canvasTexture.width, canvasTexture.height, TextureFormat.RGBA32, false);
			tex.ReadPixels(new Rect(0, 0, canvasTexture.width, canvasTexture.height), 0, 0);
			tex.Apply();
			RenderTexture.active = null;

			Color compareColor = gamePhase == GamePhase.ClearRust ? cleanColor : paintedColor;

			if (gamePhase == GamePhase.ClearRust)
			{
				foreach (Vector2 currentPosition in maskPositions.ToArray())
				{
					Color currentColor = tex.GetPixel((int)currentPosition.x, (int)currentPosition.y);

					if (IsThisPixelOk(currentColor, compareColor, 0.002f))
					{
						paintedPixelCount++;
						maskPositions.Remove(currentPosition);
					}
				}
			}
			else if (gamePhase == GamePhase.Painting)
			{
				foreach (Vector2 currPosition in sprayMaskPositions.ToArray())
				{
					Color currColor = tex.GetPixel((int)currPosition.x, (int)currPosition.y);

					if (IsThisPixelOk(currColor, compareColor, 0.002f))
					{
						paintedPixelCount++;
						sprayMaskPositions.Remove(currPosition);
					}
				}
			}

			int percent = (paintedPixelCount * 100 / maskPixelCount);
			txtPercent.text = "%" + percent + " completed";

			if (gamePhase == GamePhase.ClearRust && percent >= cleanPercent)
			{
				baseMaterial.mainTexture = cleanTexture;
				goSponge.SetActive(false);
				spray.Show();
				spray.UpdateParticleColor(paintedColor);
				brushColor = paintedColor;
				gamePhase = GamePhase.Painting;
				paintedPixelCount = 0;
				saving = true;
				Invoke("SaveTexture", 0.1f);

				if (Input.GetMouseButton(0))
					spray.StartSpray();
			}
			else if (gamePhase == GamePhase.Painting && percent >= paintedPercent)
			{
				endGameUI.SetActive(true);
				baseMaterial.mainTexture = paintedTexture;
				spray.Hide();
				spray.StopSpray();
				gamePhase = GamePhase.Finished;
				saving = true;
				Invoke("SaveTexture", 0.1f);

				cameraPivot.GetComponent<ModelViewControls>().enabled = false;
				goPaintObject.GetComponent<ObjectInspector>().enabled = true;
				RestoreCameraData();
			}

			needToCheckPaintedPercent = false;
		}
	}

	bool IsThisPixelOk(Color mainColor, Color compareColor, float treshold)
    {
		Color color1 = compareColor;
		Color color2 = compareColor;

		color1.r += treshold;
		color1.g += treshold;
		color1.b += treshold;

		color2.r -= treshold;
		color2.g -= treshold;
		color2.b -= treshold;

		bool isRedOk = mainColor.r >= color2.r && mainColor.r <= color1.r;
		bool isGreenOk = mainColor.g >= color2.g && mainColor.g <= color1.g;
		bool isBlueOk = mainColor.b >= color2.b && mainColor.b <= color1.b;

		return isRedOk && isGreenOk && isBlueOk;
	}

	//The main action, instantiates a brush or decal entity at the clicked position on the UV map
	void DoAction(){	
		if (saving)
			return;
		Vector3 uvWorldPosition=Vector3.zero;		
		if(HitTestUVPosition(ref uvWorldPosition)){
			GameObject brushObj;
			//if(gamePhase == GamePhase.ClearRust)
			//{
				brushObj=(GameObject)Instantiate(Resources.Load("TexturePainter-Instances/SpongeBrushEntity")); //Paint a brush
				brushObj.GetComponent<SpriteRenderer>().color = brushColor; //Set the brush color
			/*}
			else
			{
				brushObj=(GameObject)Instantiate(Resources.Load("TexturePainter-Instances/BrushEntity")); //Paint a decal
				brushObj.GetComponent<SpriteRenderer>().color = brushColor;
			}*/

			brushColor.a = brushSize * 2.0f; // Brushes have alpha to have a merging effect when painted over.
			brushObj.transform.parent = brushContainer.transform; //Add the brush to our container to be wiped later
			brushObj.transform.localPosition = uvWorldPosition; //The position of the brush (in the UVMap)
			brushObj.transform.localScale = Vector3.one * brushSize;//The size of the brush
		}
		brushCounter++; //Add to the max brushes
		if (brushCounter >= MAX_BRUSH_COUNT)
		{ //If we reach the max brushes available, flatten the texture and clear the brushes
			brushCursor.SetActive (false);
			saving=true;
			Invoke("SaveTexture",0.1f);
		}
	}
	//To update at realtime the painting cursor on the mesh
	void UpdateBrushCursor(){
		Vector3 uvWorldPosition=Vector3.zero;
		if (gamePhase == GamePhase.Painting && HitTestUVPosition (ref uvWorldPosition) && !saving) {
			brushCursor.SetActive(true);
			brushCursor.transform.position =uvWorldPosition+brushContainer.transform.position;									
		} else {
			brushCursor.SetActive(false);
		}		
	}
	//Returns the position on the texuremap according to a hit in the mesh collider
	bool HitTestUVPosition(ref Vector3 uvWorldPosition){
		RaycastHit hit;
		Vector3 cursorPos = new Vector3 (Input.mousePosition.x, Input.mousePosition.y, 0.0f);
		Ray cursorRay=sceneCamera.ScreenPointToRay (cursorPos);
		if (Physics.Raycast(cursorRay,out hit,200)){
			MeshCollider meshCollider = hit.collider as MeshCollider;
			if (meshCollider == null || meshCollider.sharedMesh == null)
				return false;			
			Vector2 pixelUV  = new Vector2(hit.textureCoord.x,hit.textureCoord.y);
			uvWorldPosition.x=pixelUV.x-canvasCam.orthographicSize;//To center the UV on X
			uvWorldPosition.y=pixelUV.y-canvasCam.orthographicSize;//To center the UV on Y
			uvWorldPosition.z=0.0f;
			return true;
		}
		else{		
			return false;
		}
		
	}
	//Sets the base material with a our canvas texture, then removes all our brushes
	void SaveTexture(){		
		brushCounter=0;
		System.DateTime date = System.DateTime.Now;
		RenderTexture.active = canvasTexture;
		Texture2D tex = new Texture2D(canvasTexture.width, canvasTexture.height, TextureFormat.RGBA32, false);		
		tex.ReadPixels(new Rect (0, 0, canvasTexture.width, canvasTexture.height), 0, 0);
		tex.Apply ();
		RenderTexture.active = null;
		baseMaterial.mainTexture =tex;	//Put the painted texture as the base
		foreach (Transform child in brushContainer.transform) {//Clear brushes
			Destroy(child.gameObject);
		}
		//StartCoroutine ("SaveTextureToFile"); //Do you want to save the texture? This is your method!
		Invoke ("ShowCursor", 0.1f);
	}
	//Show again the user cursor (To avoid saving it to the texture)
	void ShowCursor(){	
		saving = false;
	}

	////////////////// PUBLIC METHODS //////////////////

	public void SetBrushMode(Painter_BrushMode brushMode){ //Sets if we are painting or placing decals
		mode = brushMode;
		brushCursor.GetComponent<SpriteRenderer> ().sprite = brushMode == Painter_BrushMode.PAINT ? cursorPaint : cursorDecal;
	}
	public void SetBrushSize(float newBrushSize){ //Sets the size of the cursor brush or decal
		brushSize = newBrushSize;
		brushCursor.transform.localScale = Vector3.one * brushSize;
	}

	////////////////// OPTIONAL METHODS //////////////////

	#if !UNITY_WEBPLAYER 
		IEnumerator SaveTextureToFile(Texture2D savedTexture){		
			brushCounter=0;
			string fullPath=System.IO.Directory.GetCurrentDirectory()+"\\UserCanvas\\";
			System.DateTime date = System.DateTime.Now;
			string fileName = "CanvasTexture.png";
			if (!System.IO.Directory.Exists(fullPath))		
				System.IO.Directory.CreateDirectory(fullPath);
			var bytes = savedTexture.EncodeToPNG();
			System.IO.File.WriteAllBytes(fullPath+fileName, bytes);
			Debug.Log ("<color=orange>Saved Successfully!</color>"+fullPath+fileName);
			yield return null;
		}
	#endif
}
