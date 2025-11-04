using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using System.Collections;

public class TestTest : MonoBehaviour
{
    public GameObject[] ballColors; // prefab màu bóg
    public GameObject[] numTubes; // prefab số ống
    public List<GameObject>[] tubes; 

    public float ballSpacing = 1.2f; // khoảng cách bóg
    public int tubeCapacity = 4; // bóng/ống

    public GameObject winMenu;

    Stack<(GameObject, int, int)> undoList = new Stack<(GameObject, int, int)>(); //lịch sử di chuyển bóng


    private void Start()
    {
        GenerateBalls();
        CameraSize();
    }

    private void Update()
    {
        // gọi hàm khi ấn 
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                RaycastHit2D x = Physics2D.Raycast(mousePosition, Vector2.zero);

                if (x.collider != null)
                {
                    for (int i = 0; i < numTubes.Length; i++)
                    {
                        if (x.collider.gameObject == numTubes[i])
                        {
                            ClickTube(i);
                            break;
                        }
                    }
                }
            }
            
        }
        CheckWin();
    }

    private int click = 99; 
    private void ClickTube(int tubeIndex)
    {
        if (click == 99)
        {
            click = tubeIndex;
        }
        else
        {
            MoveBall(click, tubeIndex); //gọi hàm khi đã ấn chuột vào ống thứ 2
            click = 99; 
        }
    }
        
    private void GenerateBalls()
    {
        winMenu.SetActive(false);

        //danh sách các ống
        tubes = new List<GameObject>[numTubes.Length];
        for (int i = 0; i < numTubes.Length; i++)
        {
            tubes[i] = new List<GameObject>();
        }

        List<GameObject> balls = new List<GameObject>(); // danh sách chứa bóng

        // tạo số lượng bóng theo màu sắc
        for (int color = 0; color < ballColors.Length; color++)
        {
            for (int i = 0; i < tubeCapacity; i++)
            {
                balls.Add(ballColors[color]);
            }
        }

        // rondom bóng
        Random random = new Random();
        for (int i = balls.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (balls[i], balls[j]) = (balls[j], balls[i]);
        }

        // chia bóng vào ống
        int index = 0;
        for (int i = 0; i < numTubes.Length - 2; i++)
        {
            for (int j = 0; j < tubeCapacity; j++)
            {
                GameObject ball = Instantiate(balls[index++], numTubes[i].transform);
                Vector3 pos = numTubes[i].transform.position + new Vector3(0, (j * ballSpacing) - ((tubeCapacity - 1) * ballSpacing), 0);
                ball.transform.position = pos;

                tubes[i].Add(ball); 
            }
        }
    }

    public bool MoveBall(int tubeA, int tubeB)
    {
        // ống hợp lệ
        if (tubeA < 0 || tubeA >= numTubes.Length || tubeB < 0 || tubeB >= numTubes.Length)
            return false;

        List<GameObject> tubeAList = tubes[tubeA];
        List<GameObject> tubeBList = tubes[tubeB];

        // ống đầu vào != null
        if (tubeAList.Count == 0)
            return false;

        // ống đích != full
        if (tubeBList.Count >= tubeCapacity)
            return false;

        GameObject ballToMove = tubeAList[tubeAList.Count - 1]; // lấy bóng trên nhất

        // xem có thể di chuyển qua không
        if (tubeBList.Count > 0 && tubeBList[tubeBList.Count - 1].tag != ballToMove.tag)
            return false;

        StartCoroutine(MoveBallAnimation(ballToMove, tubeA, tubeB)); // Gọi hàm di chuyển bóng mượt
        undoList.Push((ballToMove, tubeA, tubeB)); // lưu lịch sử
        return true;
    }

    // hàm di chuyển bóng từ ống này qua ống khác
    private IEnumerator MoveBallAnimation(GameObject ball, int tubeA, int tubeB)
    {
        List<GameObject> tubeListA = tubes[tubeA];
        List<GameObject> tubeListB = tubes[tubeB];

        Transform tubeTransformB = numTubes[tubeB].transform;
        Vector3 startPos = ball.transform.position;
        Vector3 upPos = startPos + new Vector3(0, 1.5f + (numTubes[tubeA].transform.position.y - startPos.y), 0);
        Vector3 targetPos = tubeTransformB.position + new Vector3(0, 1.5f, 0);

        int ballCountBeforeAdding = tubeListB.Count;
        Vector3 bottomPos = tubeTransformB.position + new Vector3(0, -(tubeCapacity - 1) * ballSpacing, 0);
        Vector3 finalPos = bottomPos + new Vector3(0, ballCountBeforeAdding * ballSpacing, 0);

        tubeListA.RemoveAt(tubeListA.Count - 1);
        tubeListB.Add(ball);

        float moveTime = 0.3f;
        ball.transform.SetParent(numTubes[tubeB].transform);

        yield return MoveOverTime(ball, upPos, moveTime);
        yield return MoveOverTime(ball, targetPos, moveTime);
        yield return MoveOverTime(ball, finalPos, moveTime);
        //MoveBall(tubeA, tubeB);
    }

    // hàm nội suy di chuyển
    private IEnumerator MoveOverTime(GameObject obj, Vector3 target, float duration)
    {
        Vector3 start = obj.transform.position;
        float timex = 0;

        while (timex < duration)
        {
            obj.transform.position = Vector3.Lerp(start, target, timex / duration);
            timex += Time.deltaTime;
            yield return null;
        }
        obj.transform.position = target;
    }

    public void ResetGame()
    {
        for (int i = 0; i < tubes.Length; i++)
        {
            foreach (GameObject ball in tubes[i]) { Destroy(ball); }
            tubes[i].Clear();
        }
        undoList.Clear();
        GenerateBalls();
        click = 99;
    }

    public void Undo()
    {
        if (undoList.Count > 0)
        {
            var x = undoList.Pop();
            StartCoroutine(MoveBallAnimation(x.Item1, x.Item3, x.Item2));
        }
    }

 

    private void CheckWin()
    {
        foreach (var z in tubes)
        {
            if (z.Count == 0) continue; // Nếu ống trống thì bỏ qua
            if (z.Count == tubeCapacity)
            {
                foreach (var ball in z)
                {
                    if (ball.tag != z[0].tag) return;
                }
            }
            else if(z.Count < tubeCapacity)
            {
                return;
            }continue;
        }
        winMenu.SetActive(true); 
    }


    void CameraSize()
    {
        float targetAspect = 16f / 9f;  // Tỷ lệ khung hình mong muốn
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        Camera cam = Camera.main;
        if (scaleHeight < 1.0f)
        {
            cam.orthographicSize = 5.0f / scaleHeight; // Điều chỉnh theo chiều cao
        }
        else
        {
            cam.orthographicSize = 5.0f; // Giữ kích thước chuẩn
        }
    }



}