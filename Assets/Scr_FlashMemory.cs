using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

using Random = UnityEngine.Random;

public class Scr_FlashMemory : MonoBehaviour {
    public KMAudio BombAudio;
    public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public KMSelectable ModuleSelect, BoltButton;
    public KMSelectable[] ModuleButtons;
    public GameObject EmpSquares, TempSquare;
    public Light[] LightLED;

    readonly Color[] flashColors = { new Color32(253, 255, 111, 255), new Color32(244, 115, 121, 255) };
    readonly Color[] squareColors = { new Color32(50, 50, 50, 255), new Color32(239, 239, 239, 255), new Color32(237, 28, 36, 255) };
    readonly int squareAmount = 2;

    List<int> squareSequence = new List<int>();
    int squareTotal, currentStage, pressedFlash, pressedSquares;
    bool checkingPass;

    bool moduleSolved;
    static int moduleIdCounter = 1;
    int moduleId;

    void Start() {
        moduleId = moduleIdCounter++;

        for (var i = 0; i < ModuleButtons.Length; i++) {
            var j = i;

            ModuleButtons[i].OnInteract += delegate() {
                OnSquarePress(j);

                return false;
            };
        }

        for (var i = 0; i < LightLED.Length; i++)
            LightLED[i].enabled = false;

        BoltButton.OnInteract += delegate() {
            OnFlashPress();

            return false;
        };
    }

    IEnumerator SetSequence() {
        for (var i = 0; i < (3 + squareTotal) * 2; i++) {
            ModuleButtons[squareSequence[i % (3 + squareTotal)]].GetComponent<Renderer>().material.color = squareColors[(i < 3 + squareTotal).ToInt()];

            if (i == 3 + squareTotal - 1) yield return new WaitForSeconds(1f);
        }

        pressedFlash = 2;
    }

    IEnumerator UnlightSquares() {
        yield return new WaitForSeconds(1f);

        for (var i = 0; i < ModuleButtons.Length; i++)
            ModuleButtons[i].GetComponent<Renderer>().material.color = squareColors[0];

        pressedFlash = pressedSquares = 0;
        BoltButton.GetComponent<Renderer>().material.color = flashColors[0];
        checkingPass = false;
    }

    void OnSquarePress(int squarePressed) {
        var compRend = ModuleButtons[squarePressed].GetComponent<Renderer>();

        if (moduleSolved || pressedFlash != 2 || compRend.material.color.Equals(squareColors[1]) || checkingPass) return;

        ModuleSelect.AddInteractionPunch(0.1f);

        if (squareSequence.Contains(squarePressed)) {
            BombAudio.PlaySoundAtTransform("Snd_Select", transform);
            compRend.material.color = squareColors[1];

            if (++pressedSquares == 3 + squareTotal) {
                StartCoroutine(UnlightSquares());
                checkingPass = true;
                LightLED[currentStage].enabled = true;

                if (++currentStage == 4) {
                    Debug.LogFormat("[Flash Memory #{0}] Module solved!", moduleId);
                    BombModule.HandlePass();
                    moduleSolved = true;
                }
            }
        } else {
            BombAudio.PlaySoundAtTransform("Snd_Fail", transform);
            compRend.material.color = squareColors[2];
            StartCoroutine(UnlightSquares());
            checkingPass = true;
            Debug.LogFormat("[Flash Memory #{0}] You entered an incorrect sequence. Resetting module.", moduleId);
            BombModule.HandleStrike();

            for (var i = 0; i < LightLED.Length; i++)
                LightLED[i].enabled = false;

            currentStage = 0;
        }
    }

    void OnFlashPress() {
        if (moduleSolved || pressedFlash == 1 || checkingPass) return;

        BombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        ModuleSelect.AddInteractionPunch(0.5f);

        if (pressedFlash == 2) {
            BombModule.HandleStrike();
            pressedSquares = 0;
        } else {
            squareTotal = squareAmount * currentStage;
            squareSequence.Clear();

            for (var i = 0; i < 3 + squareTotal; i++) {
                var checkSquare = 0;

                do {
                    checkSquare = Random.Range(0, ModuleButtons.Length);
                } while (squareSequence.Contains(checkSquare));

                squareSequence.Add(checkSquare);
            }

            Debug.LogFormat("[Flash Memory #{0}] Set of squares for stage {1} is: {2}", moduleId, currentStage + 1, squareSequence.Select(x => x.ToCoord(4)).Join(", "));
        }

        BombAudio.PlaySoundAtTransform("Snd_Reveal", transform);
        StartCoroutine(SetSequence());
        BoltButton.GetComponent<Renderer>().material.color = flashColors[1];
        pressedFlash = 1;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press A1 B3 C5... (column [A to D] and row [1 to 4] to press) | !{0} bolt (presses the lightning bolt button)";
#pragma warning restore 414

    KMSelectable[] ProcessTwitchCommand(string command) {
        command = command.ToLowerInvariant().Trim();

        if (Regex.IsMatch(command, @"^press +[a-d1-4^, |&]+$")) {
            command = command.Substring(6).Trim();

            var presses = command.Split(new[] { ',', ' ', '|', '&' }, StringSplitOptions.RemoveEmptyEntries);
            var pressList = new List<KMSelectable>();

            for (int i = 0; i < presses.Length; i++) {
                if (Regex.IsMatch(presses[i], @"^[a-d][1-4]$")) {
                    var setPress = presses[i][0] - 'a' + (4 * (presses[i][1] - '1'));
                    pressList.Add(ModuleButtons[setPress]);
                }
            }

            return (pressList.Count > 0) ? pressList.ToArray() : null;
        }

        return (Regex.IsMatch(command, @"^bolt$")) ? new[] { BoltButton } : null;
    }
}