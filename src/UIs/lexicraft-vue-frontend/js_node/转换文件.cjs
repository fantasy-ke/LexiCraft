let path = require("path");
let fs = require("fs");

const RESULT_DIR = path.join(__dirname, 'source');
const RESULT2_DIR = path.join(__dirname, 'result');


function getDefaultWord(val) {
  return {
    custom: false,
    id: 0,
    "word": "",
    "phonetic0": "",
    "phonetic1": "",
    "trans": [],
    "sentences": [],
    "phrases": [],
    "synos": [],
    "relWords": {
      "root": "",
      "rels": []
    },
    "etymology": [],
    ...val
  }
}

const safeString = (str) => (typeof str === 'string' ? str.trim() : '');
const safeSplit = (str, sep) =>
  safeString(str) ? safeString(str).split(sep).filter(Boolean) : [];


(async () => {
  const files = fs.readdirSync(RESULT_DIR).filter(f => f.endsWith('.json'));
  for (const file of files) {
    const filePath = path.join(RESULT_DIR, file);
    const raw = JSON.parse(fs.readFileSync(filePath, 'utf-8'));
    console.log(raw.length)
    let list = raw.map((v, i) => {

      const trans = v.trans.map(line => {
        const regex = /(n|vt|vi|adj)\.\s*([^vna]+?)(?=\s*(n\.|vt\.|vi\.|adj\.|$))/g;
        let result = [];
        let match1;
        while ((match1 = regex.exec(line)) !== null) {
          result.push(match1[1] + ". " + match1[2].trim());
        }
        if (!result.length) {
          return [line]
        }
        return result;
      }).flat().map(line => {

        const match = line.match(/^([^\s.]+\.?)\s*(.*)$/);
        if (match) {
          let pos = safeString(match[1]);
          let cn = safeString(match[2]);

          // 如果 pos 不是常规词性（不以字母开头），例如 "【名】"
          if (!/^[a-zA-Z]+\.?$/.test(pos)) {
            cn = safeString(line); // 整行放到 cn
            pos = ''; // pos 置空
          }

          return {pos, cn};
        }
        return {pos: '', cn: safeString(line)};
      });

      return getDefaultWord({
        word: v.name,
        phonetic0: v.usphone || '',
        phonetic1: v.ukphone || '',
        trans,
      })
    })
    fs.writeFileSync(path.join(RESULT2_DIR, file), JSON.stringify(list, null, 2), 'utf-8');
  }
})();
