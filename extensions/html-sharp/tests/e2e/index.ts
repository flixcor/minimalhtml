import * as path from "node:path";
import * as fs from "node:fs";
import Mocha from "mocha";

export function run(): Promise<void> {
  const mocha = new Mocha({ ui: "bdd", color: true, timeout: 60_000 });

  const testsRoot = __dirname;

  return new Promise((resolve, reject) => {
    const files = fs
      .readdirSync(testsRoot)
      .filter((f) => f.endsWith(".test.js"))
      .map((f) => path.resolve(testsRoot, f));

    for (const f of files) {
      mocha.addFile(f);
    }

    try {
      mocha.run((failures) => {
        if (failures > 0) {
          reject(new Error(`${failures} test(s) failed.`));
        } else {
          resolve();
        }
      });
    } catch (err) {
      reject(err);
    }
  });
}
