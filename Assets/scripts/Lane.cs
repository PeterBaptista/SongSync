using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Lane : MonoBehaviour
{
    public int noteRestriction;
    public SongManager songManager;
    public KeyCode input;
    public GameObject notePrefab;
    List<Note> notes = new List<Note>();
    public Color laneColor;
    public List<NoteData> timeStamps = new List<NoteData>();
    private SpriteRenderer hitboxRenderer;
    private bool isInLongNote = false;


    int spawnIndex = 0;
    int inputIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        hitboxRenderer = transform.Find("Hitbox").GetComponent<SpriteRenderer>();
        hitboxRenderer.color = laneColor;
    }
    private List<Melanchall.DryWetMidi.Interaction.Note> RemoveOverlappingNotes(ICollection<Melanchall.DryWetMidi.Interaction.Note> notes)
    {
        var allNotesArray = new Melanchall.DryWetMidi.Interaction.Note[notes.Count];
        notes.CopyTo(allNotesArray, 0);
        var filteredNotesArray = new List<Melanchall.DryWetMidi.Interaction.Note>();

        foreach(var note in allNotesArray)
        {
            if (note.NoteNumber == noteRestriction)
            {
                if (filteredNotesArray.Count > 0 && filteredNotesArray[^1].Time <= note.Time && filteredNotesArray[^1].EndTime >= note.Time) continue;
                else filteredNotesArray.Add(note);
            }
        }

        return filteredNotesArray;
    }
    public void SetTimeStamps(ICollection<Melanchall.DryWetMidi.Interaction.Note> notes)
    {
        var notesArray = RemoveOverlappingNotes(notes);

        foreach (var note in notesArray)
        {
            if (note.NoteNumber == noteRestriction)
            {
                var metricTimeSpan = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, SongManager.midiFile.GetTempoMap());
                var metricTimeSpanDuration = TimeConverter.ConvertTo<MetricTimeSpan>(note.Length, SongManager.midiFile.GetTempoMap());
                var duration = (double)metricTimeSpanDuration.Minutes * 60f + metricTimeSpanDuration.Seconds + (double)metricTimeSpanDuration.Milliseconds / 1000f;
                if (duration < SongManager.Instance.minLongNoteDuration) duration = 0;
                timeStamps.Add(new NoteData((double)metricTimeSpan.Minutes * 60f + metricTimeSpan.Seconds + (double)metricTimeSpan.Milliseconds / 1000f, duration));
            }
        }
    }
    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(input))
        {
            hitboxRenderer.color = Color.black;
        }

        if (Input.GetKeyUp(input))
        {
            hitboxRenderer.color = laneColor;
        }


        if (spawnIndex < timeStamps.Count)
        {

            if (SongManager.GetAudioSourceTime() >= timeStamps[spawnIndex].timestamp - SongManager.Instance.noteTime)
            {
                var note = Instantiate(notePrefab, transform);
                notes.Add(note.GetComponent<Note>());
                note.GetComponent<Note>().assignedTime = (float)timeStamps[spawnIndex].timestamp;
                note.GetComponent<Note>().duration = (float)timeStamps[spawnIndex].duration;
                Vector3 notePosition = note.transform.localPosition;
                notePosition.y = SongManager.Instance.noteSpawnY;
                note.transform.localPosition = notePosition;

                var childTransform = note.transform.GetChild(0);
                Vector3 scale = childTransform.localScale;
                scale.y = ((float) timeStamps[spawnIndex].duration * SongManager.Instance.noteDistanceToTap) / SongManager.Instance.noteTime;
                childTransform.localScale = scale;

                float halfHeight = scale.y / 2.0f;
                Vector3 newPosition = childTransform.localPosition;
                newPosition.y = halfHeight;
                childTransform.localPosition = newPosition;

                note.GetComponent<SpriteRenderer>().color = laneColor;
                childTransform.GetComponent<SpriteRenderer>().color = laneColor;

                note.GetComponent<SpriteRenderer>().enabled = true;
                childTransform.GetComponent<SpriteRenderer>().enabled = true;

                spawnIndex++;
            }
        }

        if (inputIndex < timeStamps.Count)
        {
            double timeStamp = timeStamps[inputIndex].timestamp;
            double endTimeStamp = timeStamps[inputIndex].timestamp + timeStamps[inputIndex].duration;
            double duration = timeStamps[inputIndex].duration;
            double marginOfError = SongManager.Instance.marginOfError;
            double audioTime = SongManager.GetAudioSourceTime() - (SongManager.Instance.inputDelayInMilliseconds / 1000.0);
            double remainingDuration = endTimeStamp - audioTime > 0 ? endTimeStamp - audioTime : 0;

            // Pressionou uma tecla
            if (Input.GetKeyDown(input))
            {
                // Pressionou no tempo certo?
                if (Math.Abs(audioTime - timeStamp) < marginOfError)
                {
                    Hit();
                    Point(100);
                    print($"Hit on {inputIndex} note");

                    // Se for uma nota curta, pode deletar o objeto e seguir pra verificar a próxima nota
                    if (duration == 0)
                    {
                        Destroy(notes[inputIndex].gameObject);
                        inputIndex++;
                    }
                    else
                    {
                        notes[inputIndex].GetComponent<Note>().isPressing = true;
                        isInLongNote = true; // Caso contrário, salvar que o usuário está segurando a tecla
                    }
                }
                else
                {
                    print($"Hit inaccurate on {inputIndex} note with {Math.Abs(audioTime - timeStamp)} delay");
                }
            }
            // O usuário soltou a tecla e tava em uma nota longa
            if (Input.GetKeyUp(input) && isInLongNote)
            {
                notes[inputIndex].GetComponent<Note>().isPressing = false;
                isInLongNote = false;
                Destroy(notes[inputIndex].gameObject);
                inputIndex++;
            }

            // Passou do tempo que ele podia pressionar e não tava dentro de uma nota longa, então ele errou
            if (timeStamp + marginOfError <= audioTime && !isInLongNote)
            {
                Miss();
                inputIndex++;
            }

            // Passou da duração da nota, então não tá mais dentro de uma nota longa
            if (endTimeStamp + marginOfError <= audioTime && isInLongNote)
            {
                isInLongNote = false;
                notes[inputIndex].GetComponent<Note>().isPressing = false;
                // Vai pra próxima nota
                Destroy(notes[inputIndex].gameObject);
                inputIndex++;
            }
            else if (isInLongNote) // Tá dentro da nota longa no tempo correto, ganha ponto
            {
                Point();

                // Diminui o tamanho da nota longa pra dar a sensação que tá entrando na hitbox
                var childTransform = notes[inputIndex].gameObject.transform.GetChild(0);
                Vector3 scale = childTransform.localScale;
                scale.y = ((float)remainingDuration * SongManager.Instance.noteDistanceToTap) / SongManager.Instance.noteTime;
                childTransform.localScale = scale;

                float halfHeight = scale.y / 2.0f;
                Vector3 newPosition = childTransform.localPosition;
                newPosition.y = halfHeight;
                childTransform.localPosition = newPosition;

                var longNotePressedTransform = notes[inputIndex].gameObject.transform.GetChild(1);
                Vector3 scale2 = longNotePressedTransform.localScale;
                scale2.y = scale.y;
                longNotePressedTransform.localScale = scale2;
                longNotePressedTransform.localPosition = newPosition;
            }
        }

    }
    private void Hit()
    {
        ScoreManager.Instance.Hit();
    }
    private void Miss()
    {
        ScoreManager.Instance.Miss();
    }

    private void Point(int points = 1)
    {
        ScoreManager.Instance.Point(points);
    }
}
