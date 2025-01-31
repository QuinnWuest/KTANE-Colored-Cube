﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class ColoredCube : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMColorblindMode Colorblind;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    public MeshRenderer cubeMeshRenderer;
    public TextMesh colorblindText;
    public TextMesh indexText;

    public KMSelectable CubeButton;
    public KMSelectable BackFace;
    public KMSelectable RightFace;
    public KMSelectable FrontFace;
    public KMSelectable LeftFace;
    public KMSelectable ResetButton;

    Color[] colorList = { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.white, Color.black };
    string[] colorNamesList = { "R", "G", "B", "Y", "M", "W", "K" };
    string[] colorFullNamesList = { "Red", "Green", "Blue", "Yellow", "Magenta", "White", "Black" };

    List<int> grid = new List<int>
    {0, 4, 2, 5, 1, 6, 3,
     5, 6, 0, 1, 3, 2, 4,
     6, 2, 3, 4, 0, 5, 1,
     1, 3, 4, 2, 6, 0, 5,
     4, 0, 6, 3, 5, 1, 2,
     3, 5, 1, 0, 2, 4, 6,
     2, 1, 5, 6, 4, 3, 0};

    int curIndex;
    int curPosition;
    int startPosition;
    int targetTime;
    List<int> colorIndexes = new List<int>() { };
    List<int> targetPositions = new List<int>() { };
    List<int> startTargetPositions = new List<int>() { };

    bool colorblindActive;
    bool moving;

    void Awake()
    {
        ModuleId = ModuleIdCounter++;

        CubeButton.OnInteract += delegate () { MiddlePress(); return false; };
        BackFace.OnInteract += delegate () { BackPress(); return false; };
        RightFace.OnInteract += delegate () { RightPress(); return false; };
        FrontFace.OnInteract += delegate () { FrontPress(); return false; };
        LeftFace.OnInteract += delegate () { LeftPress(); return false; };
        ResetButton.OnInteract += delegate () { ResetPress(); return false; };

        colorblindActive = Colorblind.ColorblindModeActive;
        ColorblindSet();
    }

    void ColorblindSet()
    {
        colorblindText.gameObject.SetActive(colorblindActive);
    }

    void ResetPress()
    {
        if (ModuleSolved)
        {
            return;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, CubeButton.transform);
        curIndex = 0;
        moving = false;
        curPosition = startPosition;
        targetPositions = new List<int>(startTargetPositions);

        Debug.LogFormat("[Colored Cube #{0}] The reset button was pressed. Resetting the cube, current position: {1}", ModuleId, "ABCDEFG"[curPosition % 7].ToString() + (curPosition / 7 + 1).ToString());
        CubeCycle();
    }

    void MiddlePress()
    {
        CubeButton.AddInteractionPunch();
        if (ModuleSolved)
        {
            return;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, CubeButton.transform);

        Debug.LogFormat("[Colored Cube #{0}] The middle button was pressed at the last digit of the timer being {1}.", ModuleId, Math.Floor(Bomb.GetTime() % 60 % 10));
        if (Math.Floor(Bomb.GetTime() % 60 % 10) == targetTime && !moving)
        {
            Debug.LogFormat("[Colored Cube #{0}] The last digit of the timer matched the number gotten from the table, cycling the cube...", ModuleId);
            curIndex++;
            curIndex %= 3;
            CubeCycle();
        }
        else
        {
            Debug.LogFormat("[Colored Cube #{0}] The last digit of the timer did not match the number gotten from the table/moving has started, submitting current position.", ModuleId);
            Submit();
        }
    }

    void Submit()
    {
        Debug.LogFormat("[Colored Cube #{0}] Submitted position is {1}.", ModuleId, "ABCDEFG"[curPosition % 7].ToString() + (curPosition / 7 + 1).ToString());
        if (targetPositions.Contains(curPosition))
        {
            Debug.LogFormat("[Colored Cube #{0}] Submitted position is one of the target positions. Correct!", ModuleId);
            targetPositions.Remove(curPosition);
            if (targetPositions.Count == 0)
            {
                Debug.LogFormat("[Colored Cube #{0}] All target positions were submitted, the module solved!", ModuleId);
                GetComponent<KMBombModule>().HandlePass();
                ModuleSolved = true;
            }
        }
        else
        {
            Debug.LogFormat("[Colored Cube #{0}] Submitted position is not one of the target positions, strike!", ModuleId);
            GetComponent<KMBombModule>().HandleStrike();
        }
    }

    void BackPress()
    {
        if (ModuleSolved)
        {
            return;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, BackFace.transform);

        moving = true;

        if (curPosition - 7 < 0)
        {
            Debug.LogFormat("[Colored Cube #{0}] The back face of the cube was pressed, but there is a wall in that direction, Strike!", ModuleId);
            GetComponent<KMBombModule>().HandleStrike();
        }
        else
        {
            if (grid[curPosition - 7] == colorIndexes[1] && !targetPositions.Contains(curPosition - 7))
            {
                Debug.LogFormat("[Colored Cube #{0}] The back face of the cube was pressed, but the cell in that direction is labeled with second color of the sequence, Strike!", ModuleId);
                GetComponent<KMBombModule>().HandleStrike();
            }
            else
            {
                curPosition -= 7;
                Debug.LogFormat("[Colored Cube #{0}] The back face of the cube was pressed, moving up, current position: {1}", ModuleId, "ABCDEFG"[curPosition % 7].ToString() + (curPosition / 7 + 1).ToString());
            }        
        }
    }

    void RightPress()
    {
        if (ModuleSolved)
        {
            return;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, RightFace.transform);

        moving = true;

        if ((curPosition + 1) % 7 == 0)
        {
            Debug.LogFormat("[Colored Cube #{0}] The right face of the cube was pressed, but there is a wall in that direction, Strike!", ModuleId);
            GetComponent<KMBombModule>().HandleStrike();
        }
        else
        {
            if (grid[curPosition + 1] == colorIndexes[1] && !targetPositions.Contains(curPosition + 1))
            {
                Debug.LogFormat("[Colored Cube #{0}] The right face of the cube was pressed, but the cell in that direction is labeled with second color of the sequence, Strike!", ModuleId);
                GetComponent<KMBombModule>().HandleStrike();
            }
            else
            {
                curPosition += 1;
                Debug.LogFormat("[Colored Cube #{0}] The right face of the cube was pressed, moving right, current position: {1}", ModuleId, "ABCDEFG"[curPosition % 7].ToString() + (curPosition / 7 + 1).ToString());
            }
        }
    }

    void FrontPress()
    {
        if (ModuleSolved)
        {
            return;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, FrontFace.transform);

        moving = true;

        if (curPosition + 7 > 48)
        {
            Debug.LogFormat("[Colored Cube #{0}] The front face of the cube was pressed, but there is a wall in that direction, Strike!", ModuleId);
            GetComponent<KMBombModule>().HandleStrike();
        }
        else
        {
            if (grid[curPosition + 7] == colorIndexes[1] && !targetPositions.Contains(curPosition + 7))
            {
                Debug.LogFormat("[Colored Cube #{0}] The front face of the cube was pressed, but the cell in that direction is labeled with second color of the sequence, Strike!", ModuleId);
                GetComponent<KMBombModule>().HandleStrike();
            }
            else
            {
                curPosition += 7;
                Debug.LogFormat("[Colored Cube #{0}] The front face of the cube was pressed, moving down, current position: {1}", ModuleId, "ABCDEFG"[curPosition % 7].ToString() + (curPosition / 7 + 1).ToString());
            }
        }
    }

    void LeftPress()
    {
        if (ModuleSolved)
        {
            return;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, LeftFace.transform);

        moving = true;

        if (curPosition % 7 == 0)
        {
            Debug.LogFormat("[Colored Cube #{0}] The left face of the cube was pressed, but there is a wall in that direction, Strike!", ModuleId);
            GetComponent<KMBombModule>().HandleStrike();
        }
        else
        {
            if (grid[curPosition - 1] == colorIndexes[1] && !targetPositions.Contains(curPosition - 1))
            {
                Debug.LogFormat("[Colored Cube #{0}] The left face of the cube was pressed, but the cell in that direction is labeled with second color of the sequence, Strike!", ModuleId);
                GetComponent<KMBombModule>().HandleStrike();
            }
            else
            {
                curPosition -= 1;
                Debug.LogFormat("[Colored Cube #{0}] The left face of the cube was pressed, moving left, current position: {1}", ModuleId, "ABCDEFG"[curPosition % 7].ToString() + (curPosition / 7 + 1).ToString());
            }
        }
    }

    void Start()
    {
        colorIndexes.Add(Rnd.Range(0, 7));
        colorIndexes.Add(Rnd.Range(0, 7));
        colorIndexes.Add(Rnd.Range(0, 7));
        Debug.LogFormat("[Colored Cube #{0}] Generated colors are {1}, {2}, {3}.", ModuleId, colorFullNamesList[colorIndexes[0]], colorFullNamesList[colorIndexes[1]], colorFullNamesList[colorIndexes[2]]);

        startPosition = 7 * colorIndexes[1] + colorIndexes[2];
        curPosition = startPosition;
        Debug.LogFormat("[Colored Cube #{0}] Starting position is {1} in the grid.", ModuleId, "ABCDEFG"[colorIndexes[2]].ToString() + (colorIndexes[1] + 1).ToString());

        CubeCycle();
        CalculateTime();
        CalculateTargetPositions();
        targetPositions = startTargetPositions;
    }

    void CalculateTime()
    {
        switch (colorIndexes[0])
        {
            case 0:
                targetTime = (Bomb.GetSerialNumberLetters().ToList().Count + 8) % 10;
                break;
            case 1:
                targetTime = (Bomb.GetSerialNumberNumbers().ToList().Count + 3) % 10;
                break;
            case 2:
                targetTime = (Bomb.GetOffIndicators().Count() + 1) % 10;
                break;
            case 3:
                targetTime = (Bomb.GetBatteryCount() + 5) % 10;
                break;
            case 4:
                targetTime = (Bomb.GetPortCount() + 9) % 10;
                break;
            case 5:
                targetTime = (Bomb.GetOnIndicators().Count() + 4) % 10;
                break;
            case 6:
                targetTime = ((Bomb.GetSerialNumberNumbers().ToList())[(Bomb.GetSerialNumberNumbers()).ToList().Count - 1] + 7) % 10;
                break;
        }
        Debug.LogFormat("[Colored Cube #{0}] To cycle the cube, the middle of the cube should be pressed at the last digit of the timer being {1}.", ModuleId, targetTime);
    }

    void CalculateTargetPositions()
    {
        string serialNum = Bomb.GetSerialNumber().ToString();
        List<string> pairs = new List<string> { };
        pairs.Add(serialNum[0].ToString() + serialNum[1].ToString());
        pairs.Add(serialNum[2].ToString() + serialNum[3].ToString());
        pairs.Add(serialNum[4].ToString() + serialNum[5].ToString());

        for (int i = 0; i < 3; i++)
        {
            string curPair = pairs[i];
            List<int> curPairNumber = new List<int> { };

            for (int j = 0; j < 2; j++)
            {
                if ("ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(curPair[j].ToString()))
                {
                    curPairNumber.Add("ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(curPair[j]) + 1);
                }
                else
                {
                    if (curPair[j] == '0')
                    {
                        curPairNumber.Add(1);
                    }
                    else
                    {
                        curPairNumber.Add(Int32.Parse(curPair[j].ToString()));
                    }
                }
            }
            while (curPairNumber[0] > 7)
            {
                curPairNumber[0] -= 7;
            }
            while (curPairNumber[1] > 7)
            {
                curPairNumber[1] -= 7;
            }

            startTargetPositions.Add((curPairNumber[0] * 7 + curPairNumber[1] - 8));
        }
        Debug.LogFormat("[Colored Cube #{0}] Target positions are {1}, {2} and {3}", ModuleId, "ABCDEFG"[startTargetPositions[0] % 7].ToString() + (startTargetPositions[0] / 7 + 1).ToString(), "ABCDEFG"[startTargetPositions[1] % 7].ToString() + (startTargetPositions[1] / 7 + 1).ToString(), "ABCDEFG"[startTargetPositions[2] % 7].ToString() + (startTargetPositions[2] / 7 + 1).ToString());
    }

    void CubeCycle()
    {
        Color curColor = colorList[colorIndexes[curIndex]];
        string curColorName = colorNamesList[colorIndexes[curIndex]];

        cubeMeshRenderer.material.color = curColor;
        if (colorIndexes[curIndex] != 5 & colorIndexes[curIndex] != 6)
        {
            colorblindText.text = curColorName;
        }
        else
        {
            colorblindText.text = "";
        }

        indexText.text = (curIndex + 1).ToString();
        if (colorIndexes[curIndex] == 5)
        {
            indexText.color = Color.black;
        }
        else
        {
            indexText.color = Color.white;
        }
        Debug.LogFormat("[Colored Cube #{0}] Current color is {1}, which is position {2} in the sequence.", ModuleId, colorFullNamesList[colorIndexes[curIndex]], curIndex + 1);
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} m/mid/middle to press the middle part of the cube at any time. !{0} m/mid/middle # to press the middle part of the cube at last digit of the timer being #. !{0} move u/b/r/d/f/l to move. Directions can be chained like so: !{0} move uurlf.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        var tokens = Command.ToLowerInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length == 0)
        {
            yield break;
        }

        switch (tokens[0])
        {
            case "m":
            case "mid":
            case "middle":
                if (tokens.Length == 1)
                {
                    CubeButton.OnInteract();
                    yield return new WaitForSeconds(0.1f);
                    break;
                }
                if (!"0123456789".Contains(tokens[1]) || tokens[1].Length > 1)
                {
                    yield return null;
                    yield return "sendtochaterror Invalid press time!";
                    yield break;
                }
                string pressTime = tokens[1];
                yield return null;

                string curLastDigit = Bomb.GetFormattedTime()[Bomb.GetFormattedTime().Length - 1].ToString();
                while (!(curLastDigit == pressTime))
                {
                    yield return null;
                    curLastDigit = Bomb.GetFormattedTime()[Bomb.GetFormattedTime().Length - 1].ToString();
                }
                CubeButton.OnInteract();
                yield return new WaitForSeconds(0.1f);
                break;
            case "move":
                if (tokens.Length == 1)
                {
                    yield return null;
                    yield return "sendtochaterror No moves given!";
                    yield break;
                }
                if (tokens.Length == 2)
                {
                    foreach (char token in tokens[1])
                    {
                        switch (token.ToString().ToLowerInvariant())
                        {
                            case "u":
                            case "b":
                                BackFace.OnInteract();
                                yield return new WaitForSeconds(0.1f);
                                break;
                            case "r":
                                RightFace.OnInteract();
                                yield return new WaitForSeconds(0.1f);
                                break;
                            case "d":
                            case "f":
                                FrontFace.OnInteract();
                                yield return new WaitForSeconds(0.1f);
                                break;
                            case "l":
                                LeftFace.OnInteract();
                                yield return new WaitForSeconds(0.1f);
                                break;
                            default:
                                yield break;
                        }
                    }
                }
                break;
            default:
                yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
    }
}
