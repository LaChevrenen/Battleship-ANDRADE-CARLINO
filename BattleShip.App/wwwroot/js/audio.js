// Audio du jeu, entièrement synthétisé : aucune ressource externe, aucune licence à porter.
// Deux bus séparés, les effets courts d'un côté et la nappe d'ambiance de l'autre, pour que
// couper l'un ne touche pas l'autre.

let audioContext;
let sfxBus;
let musicBus;
let musicFilter;
let musicTimer;
let musicBar = 0;
let mood = 'calm';

const MUSIC_LEVEL = 0.62;

// Deux ambiances. La préparation respire, le combat pousse : accords plus sombres, plus courts,
// filtre plus ouvert, et une pulsation sourde qui n'existe pas dans le calme.
const MOODS = {
    calm: {
        seconds: 9,
        filter: 1150,
        pulse: 0,
        chords: [
            [146.83, 185.00, 220.00, 277.18, 329.63],
            [123.47, 146.83, 185.00, 220.00, 293.66],
            [98.00, 123.47, 146.83, 185.00, 246.94],
            [110.00, 146.83, 164.81, 196.00, 246.94],
        ],
    },
    combat: {
        seconds: 6,
        filter: 1650,
        pulse: 1.5,
        chords: [
            [146.83, 174.61, 220.00, 261.63, 349.23],
            [116.54, 146.83, 174.61, 220.00, 293.66],
            [87.31, 110.00, 130.81, 164.81, 261.63],
            [130.81, 164.81, 196.00, 233.08, 311.13],
        ],
    },
};

const audio = () => {
    const AudioContext = window.AudioContext || window.webkitAudioContext;
    if (!AudioContext) return null;

    if (!audioContext) {
        audioContext = new AudioContext();

        // Compresseur sur la sortie : la nappe, la pulsation et un effet peuvent tomber ensemble,
        // et la somme saturerait sans lui.
        const master = audioContext.createDynamicsCompressor();
        master.threshold.value = -12;
        master.knee.value = 24;
        master.ratio.value = 6;
        master.attack.value = 0.005;
        master.release.value = 0.25;
        master.connect(audioContext.destination);

        sfxBus = audioContext.createGain();
        sfxBus.gain.value = 0.6;
        sfxBus.connect(master);

        // Réverbération pauvre mais convaincante : un écho court et sombre réinjecté sur lui-même.
        // Un vrai convolver demanderait une réponse impulsionnelle à embarquer.
        const delay = audioContext.createDelay(1);
        delay.delayTime.value = 0.38;
        const feedback = audioContext.createGain();
        feedback.gain.value = 0.34;
        const damping = audioContext.createBiquadFilter();
        damping.type = 'lowpass';
        damping.frequency.value = 1800;
        const wet = audioContext.createGain();
        wet.gain.value = 0.38;

        musicFilter = audioContext.createBiquadFilter();
        musicFilter.type = 'lowpass';
        musicFilter.frequency.value = MOODS[mood].filter;
        musicFilter.Q.value = 0.7;

        musicBus = audioContext.createGain();
        musicBus.gain.value = MUSIC_LEVEL;

        musicFilter.connect(musicBus);
        musicBus.connect(master);
        musicBus.connect(delay);
        delay.connect(damping).connect(feedback).connect(delay);
        damping.connect(wet).connect(master);

        // Le filtre respire lentement : sans ce mouvement, la nappe devient une note tenue.
        const breath = audioContext.createOscillator();
        const breathDepth = audioContext.createGain();
        breath.frequency.value = 0.045;
        breathDepth.gain.value = 380;
        breath.connect(breathDepth).connect(musicFilter.frequency);
        breath.start();
    }

    if (audioContext.state === 'suspended') audioContext.resume();
    return audioContext;
};

const tone = (context, frequency, start, duration, volume, type = 'triangle') => {
    const oscillator = context.createOscillator();
    const gain = context.createGain();
    oscillator.frequency.value = frequency;
    oscillator.type = type;
    gain.gain.setValueAtTime(0.0001, start);
    gain.gain.exponentialRampToValueAtTime(volume, start + 0.025);
    gain.gain.exponentialRampToValueAtTime(0.0001, start + duration);
    oscillator.connect(gain).connect(sfxBus);
    oscillator.start(start);
    oscillator.stop(start + duration + 0.03);
};

const noise = (context, start, duration, volume, destination) => {
    const buffer = context.createBuffer(1, context.sampleRate * duration, context.sampleRate);
    const data = buffer.getChannelData(0);
    for (let index = 0; index < data.length; index++) data[index] = Math.random() * 2 - 1;
    const source = context.createBufferSource();
    const gain = context.createGain();
    source.buffer = buffer;
    gain.gain.setValueAtTime(volume, start);
    gain.gain.exponentialRampToValueAtTime(0.0001, start + duration);
    source.connect(gain).connect(destination ?? sfxBus);
    source.start(start);
};

window.battleSound = (kind) => {
    const context = audio();
    if (!context) return;

    const start = context.currentTime;
    const notes = kind === 'win' ? [523, 659, 784, 1047]
        : kind === 'sunk' ? [220, 165, 110]
        : kind === 'hit' ? [440, 554, 659]
        : kind === 'click' ? [700, 900]
        : [220];

    notes.forEach((frequency, index) =>
        tone(context, frequency, start + index * 0.07, kind === 'win' ? 0.5 : 0.36, kind === 'sunk' ? 0.48 : 0.38));

    if (kind !== 'click')
        noise(context, start, kind === 'sunk' ? 0.42 : 0.12, kind === 'sunk' ? 0.34 : 0.22);
};

// Une voix de nappe : deux oscillateurs légèrement désaccordés, entrée et sortie très longues.
// C'est le recouvrement entre deux accords qui fait qu'on n'entend aucune coupure.
const pad = (context, frequency, start, volume, seconds) => {
    const attack = seconds * 0.38;
    const hold = seconds - 1.5;
    const release = seconds * 0.5;

    [-6, 6].forEach(detune => {
        const oscillator = context.createOscillator();
        const gain = context.createGain();
        oscillator.type = 'sine';
        oscillator.frequency.value = frequency;
        oscillator.detune.value = detune;
        gain.gain.setValueAtTime(0.0001, start);
        gain.gain.linearRampToValueAtTime(volume, start + attack);
        gain.gain.setValueAtTime(volume, start + hold);
        gain.gain.linearRampToValueAtTime(0.0001, start + hold + release);
        oscillator.connect(gain).connect(musicFilter);
        oscillator.start(start);
        oscillator.stop(start + hold + release + 0.1);
    });
};

// Pulsation du combat : une frappe sourde, courte, sans hauteur marquée. Elle donne l'urgence
// sans ajouter de note, donc sans jamais jurer avec l'accord en cours.
const thump = (context, start) => {
    const oscillator = context.createOscillator();
    const gain = context.createGain();
    oscillator.type = 'sine';
    oscillator.frequency.setValueAtTime(110, start);
    oscillator.frequency.exponentialRampToValueAtTime(42, start + 0.16);
    gain.gain.setValueAtTime(0.0001, start);
    gain.gain.linearRampToValueAtTime(0.22, start + 0.012);
    gain.gain.exponentialRampToValueAtTime(0.0001, start + 0.42);
    oscillator.connect(gain).connect(musicBus);
    oscillator.start(start);
    oscillator.stop(start + 0.45);
};

const ambientBar = () => {
    const context = audio();
    if (!context) return;

    const shape = MOODS[mood];
    const start = context.currentTime + 0.05;
    const chord = shape.chords[musicBar % shape.chords.length];

    chord.forEach((frequency, index) => pad(context, frequency, start, index === 0 ? 0.14 : 0.08, shape.seconds));
    pad(context, chord[0] / 2, start, 0.11, shape.seconds);

    if (shape.pulse > 0)
        for (let beat = 0; beat * shape.pulse < shape.seconds; beat++)
            thump(context, start + beat * shape.pulse);

    // Une note isolée un accord sur deux : assez pour que ça vive, trop rare pour devenir une
    // mélodie qu'on finit par attendre.
    if (musicBar % 2 === 1) {
        const bell = chord[3] * 2;
        const oscillator = context.createOscillator();
        const gain = context.createGain();
        oscillator.type = 'triangle';
        oscillator.frequency.value = bell;
        gain.gain.setValueAtTime(0.0001, start + 1.5);
        gain.gain.linearRampToValueAtTime(0.055, start + 2);
        gain.gain.exponentialRampToValueAtTime(0.0001, start + shape.seconds - 1);
        oscillator.connect(gain).connect(musicFilter);
        oscillator.start(start + 1.5);
        oscillator.stop(start + shape.seconds - 0.8);
    }

    musicBar++;
};

const restartBars = () => {
    clearInterval(musicTimer);
    ambientBar();
    musicTimer = setInterval(ambientBar, MOODS[mood].seconds * 1000);
};

window.battleMusic = (enabled) => {
    if (!enabled) {
        clearInterval(musicTimer);
        musicTimer = null;

        // Fondu de sortie : couper net ferait claquer les nappes en cours.
        if (musicBus && audioContext) {
            const now = audioContext.currentTime;
            musicBus.gain.cancelScheduledValues(now);
            musicBus.gain.setValueAtTime(musicBus.gain.value, now);
            musicBus.gain.linearRampToValueAtTime(0.0001, now + 1.2);
        }

        return;
    }

    const context = audio();
    if (!context) return;

    const now = context.currentTime;
    musicBus.gain.cancelScheduledValues(now);
    musicBus.gain.setValueAtTime(Math.max(musicBus.gain.value, 0.0001), now);
    musicBus.gain.linearRampToValueAtTime(MUSIC_LEVEL, now + 2);

    if (musicTimer) return;
    restartBars();
};

// Passage d'une ambiance à l'autre. Les nappes déjà lancées gardent leur longue extinction : le
// fondu entre les deux se fait tout seul, sans coupure.
window.battleMusicMood = (next) => {
    if (!MOODS[next] || next === mood) return;

    mood = next;
    if (!audioContext || !musicTimer) return;

    const now = audioContext.currentTime;
    musicFilter.frequency.cancelScheduledValues(now);
    musicFilter.frequency.setValueAtTime(musicFilter.frequency.value, now);
    musicFilter.frequency.linearRampToValueAtTime(MOODS[mood].filter, now + 2.5);
    restartBars();
};

// Le navigateur interdit de produire du son avant un geste de l'utilisateur. On tente au
// chargement, et si le contexte reste suspendu, le premier geste le réveille.
window.battleAudioStart = () => {
    window.battleMusic(true);
    if (!audioContext || audioContext.state !== 'suspended') return;

    const events = ['pointerdown', 'keydown', 'touchstart'];
    const wake = () => {
        audioContext.resume();
        window.battleMusic(true);
        events.forEach(event => document.removeEventListener(event, wake));
    };

    events.forEach(event => document.addEventListener(event, wake));
};

document.addEventListener('click', event => {
    if (event.target.closest('button, a, select')) window.battleSound('click');
});
