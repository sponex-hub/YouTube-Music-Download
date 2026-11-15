const { test } = require('node:test');
const assert = require('node:assert/strict');
const { EventEmitter } = require('node:events');
const { buildArgs, downloadAudio } = require('../download');
const options = {url:'https://www.youtube.com/watch?v=BaW_jenozKc&list=example', format:'mp3', folder:__dirname, bin:require('node:path').join(__dirname,'../bin')};
test('MP3 uses actual extension and a single download without forced player clients', () => {
  const args = buildArgs(options);
  assert.ok(args.includes('--no-playlist'));
  assert.ok(args.some(s=>s.includes('%(ext)s')));
  assert.equal(args[args.indexOf('--audio-format')+1], 'mp3');
  assert.ok(!args.includes('--extractor-args'));
  assert.ok(!args.includes('--rm-cache-dir'));
});
test('WebM selects a WebM audio stream', () => {
  const args = buildArgs({...options,format:'webm'});
  assert.equal(args[args.indexOf('-f')+1], 'bestaudio[ext=webm]');
  assert.ok(!args.includes('--audio-format'));
});
test('invalid inputs are rejected', () => {
  for (const url of ['--exec=evil','file:///C:/test','https://youtube.com.evil.example/']) assert.throws(()=>buildArgs({...options,url}));
  assert.throws(()=>buildArgs({...options,format:'exe'}));
});
test('failed subprocess rejects and split progress lines are parsed', async () => {
  const progress = [];
  const launch = () => {
    const child = new EventEmitter();
    child.stdout = new EventEmitter(); child.stderr = new EventEmitter();
    process.nextTick(()=>{
      child.stdout.emit('data', 'PROG'); child.stdout.emit('data', 'RESS: 23.4%\n');
      child.stderr.emit('data', 'ERROR: test network failure'); child.emit('close',1);
    });
    return child;
  };
  await assert.rejects(downloadAudio(options,n=>progress.push(n),launch), /test network failure/);
  assert.deepEqual(progress,[23.4]);
});
