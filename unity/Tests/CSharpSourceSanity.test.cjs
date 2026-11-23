// Lightweight lexical sanity check for *all* Unity C# files.
// This is NOT a Unity C# compiler. It catches common accidental syntax
// corruption (unbalanced braces and literal \n between statements) BEFORE
// the user pays for another Unity Cloud build.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const root = path.resolve(__dirname,'..','Assets');
const files=[];
function walk(dir) {
  for(const item of fs.readdirSync(dir,{withFileTypes:true})) {
    const p=path.join(dir,item.name);
    if(item.isDirectory())walk(p);
    else if(item.name.endsWith('.cs'))files.push(p);
  }
}
walk(root);
assert.ok(files.length>=8, 'Expected to scan Unity gameplay, effects, Editor and test files');

for(const file of files) {
  const source=fs.readFileSync(file,'utf8');
  const name=path.relative(root,file);
  let state='code',line=1;
  const stack=[];
  let quote='';
  let verbatim=false;
  for(let i=0;i<source.length;i++) {
    const ch=source[i],next=source[i+1];
    if(ch==='\n'){line++;if(state==='line')state='code';continue;}
    if(state==='line')continue;
    if(state==='block') {
      if(ch==='*'&&next==='/'){state='code';i++;}
      continue;
    }
    if(state==='string') {
      if(verbatim&&ch==='"'&&next==='"'){i++;continue;}
      if(ch===quote){state='code';continue;}
      if(!verbatim&&ch==='\\') {
        assert.ok(i+1<source.length, name+': unterminated escape line '+line);
        i++;continue;
      }
      continue;
    }
    if(ch==='/'&&next==='/'){state='line';i++;continue;}
    if(ch==='/'&&next==='*'){state='block';i++;continue;}
    if(ch==='"'||ch==="'") {
      quote=ch;
      verbatim=ch==='"'&&(source[i-1]==='@'||source[i-2]==='@');
      state='string';
      continue;
    }
    if(ch==='\\') {
      assert.fail(name+': literal backslash outside C# string at line '+line);
    }
    if(ch==='('||ch==='{'||ch==='[')stack.push([ch,line]);
    const expected={')':'(', '}':'{', ']':'['}[ch];
    if(expected) {
      const top=stack.pop();
      assert.ok(top&&top[0]===expected,
        name+': mismatched '+ch+' on line '+line+'; previous '+(top||'none'));
    }
  }
  assert.equal(state,'code',name+': unterminated '+state+' at line '+line);
  assert.equal(stack.length,0,
    name+': unclosed '+stack.map(([ch,l])=>ch+' line '+l).join(','));
}
console.log('C# lexical sanity pass: '+files.length+' Unity scripts scanned. Unity Cloud compilation still required.');
