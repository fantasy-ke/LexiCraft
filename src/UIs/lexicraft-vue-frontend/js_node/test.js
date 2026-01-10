import fs from 'fs';
import path from 'path';
import {fileURLToPath} from 'url';
import pLimit from 'p-limit';
import {spawn} from 'child_process';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const defaultConcurrency = Number(process.env.CONCURRENCY || 8);
const maxRetries = Number(process.env.RETRIES || 2);
const endpoint = process.env.ENDPOINT || 'http://localhost/v1/words/addWord';
const inputFile = path.resolve(__dirname, 'save', 'all-all2.json');
const resultDir = path.resolve(__dirname, 'result');

function sleep(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

async function readWords(filePath) {
  const raw = await fs.promises.readFile(filePath, 'utf8');
  const data = JSON.parse(raw);
  if (!Array.isArray(data)) {
    throw new Error('输入文件不是数组');
  }
  return data;
}

async function postWithRetry(word, attempt = 0) {
  try {
    const res = await fetch(endpoint, {
      method: 'POST',
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify(word)
    });
    const text = await res.text().catch(() => '');
    let d = JSON.parse(text);
    if (!res.ok || !d['success']) {
      throw new Error(`HTTP ${res.status} ${res.statusText} ${text}`);
    }
    return await res.json().catch(() => ({}));
  } catch (err) {
    if (attempt < maxRetries) {
      const backoffMs = 500 * Math.pow(2, attempt);
      await sleep(backoffMs);
      return postWithRetry(word, attempt + 1);
    }
    throw err;
  }
}

async function ensureDir(dir) {
  await fs.promises.mkdir(dir, {recursive: true});
}

async function main() {
  console.log(`[start] 读取: ${inputFile}`);
  const words = await readWords(inputFile);
  console.log(`[info] 单词总数: ${words.length}`);

  const limit = pLimit(defaultConcurrency);
  let successCount = 0;
  let failCount = 0;
  const failures = [];

  let copy = words.filter(v => v.word === 'canal')


  const toCopy = JSON.stringify(copy);
  console.log(toCopy)
  return


  const tasks = words.map((word, index) =>
    limit(async () => {
      try {
        if (["”", "“"].includes(word.word[0])) word.word = word.word.slice(1)
        if (["”", "“"].includes(word.word[word.word.length - 1])) word.word = word.word.slice(0, word.word.length - 1);
        word.word = word.word.replaceAll('”', '\"')
        word.word = word.word.replaceAll('“', '\"')
        word.word = word.word.replaceAll('（', '(')
        word.word = word.word.replaceAll('）', ')')
        console.log(word.word);
        const result = await postWithRetry(word);
        successCount++;
        if ((successCount + failCount) % 100 === 0) {
          console.log(`[progress] 已处理 ${successCount + failCount}/${words.length} (成功: ${successCount}, 失败: ${failCount})`);
        }
        return {index, word, ok: true, result};
      } catch (error) {
        failCount++;
        failures.push({index, word: word.word, error: String(error)});
        if ((successCount + failCount) % 100 === 0) {
          console.log(`[progress] 已处理 ${successCount + failCount}/${words.length} (成功: ${successCount}, 失败: ${failCount})`);
        }
        return {index, word, ok: false, error: String(error)};
      }
    })
  );

  await Promise.all(tasks);

  console.log(`[done] 完成。成功: ${successCount}, 失败: ${failCount}`);

  if (failures.length > 0) {
    await ensureDir(resultDir);
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    const failFile = path.resolve(resultDir, `failed-words-${ts}.json`);
    await fs.promises.writeFile(failFile, JSON.stringify(failures, null, 2), 'utf8');
    console.log(`[save] 失败明细保存至: ${failFile}`);
  }
}

main().catch(err => {
  console.error('[error] 程序异常:', err);
  process.exitCode = 1;
});


