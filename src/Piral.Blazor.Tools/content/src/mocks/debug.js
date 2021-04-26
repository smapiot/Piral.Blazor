const { resolve, basename } = require('path');
const { proxyRequest } = require('kras/utils');

const configuration = '**MSBUILD_Configuration**';
const targetFramework = '**MSBUILD_TargetFramework**';

const bbjson = 'blazor.boot.json';
const pjson = 'package.json';
const lsjson = 'launchSettings.json';

const piralPiletFolder = resolve('.');
const rootFolder = resolve(piralPiletFolder, '..', '..');
const blazorfolderName = basename(piralPiletFolder);
const blazorprojectfolder = resolve(rootFolder, blazorfolderName);
const bbjsonPath = resolve(blazorprojectfolder, 'bin', configuration, targetFramework, 'wwwroot', '_framework', bbjson);

const appshellname = require(resolve(piralPiletFolder, pjson)).piral.name;
const appshellAppDir = resolve(require.resolve(appshellname), '..');

const originalManifest = require(resolve(appshellAppDir, '_framework', bbjson));
const piletManifest = require(bbjsonPath);

const launchSettingsPath = resolve(blazorprojectfolder, 'Properties', lsjson);

let iisUrl;
try {
  iisUrl = require(launchSettingsPath).iisSettings.iisExpress.applicationUrl;
  if (!iisUrl) throw new Error();
} catch {
  throw new Error(`Please provide launchSettings for IIS Express in ${launchSettingsPath}`);
}

const getUniqueItems = type => {
    const getLine = manifest => manifest.resources[type];
    const original = getLine(originalManifest);
    const dedicated = getLine(piletManifest);
    return Object.entries(dedicated).filter(m => !Object.keys(original).includes(m[0]));
};

const uniqueAssemblies = getUniqueItems('assembly');
const uniquePdbs = getUniqueItems('pdb');

const returnWithTweakedBBJson = res => {
    const manifestClone = JSON.parse(JSON.stringify(originalManifest));
    uniqueAssemblies.forEach(ua => (manifestClone.resources.assembly[ua[0]] = ua[1]));
    uniquePdbs.forEach(up => (manifestClone.resources.pdb[up[0]] = up[1]));

    const content = JSON.stringify(manifestClone, null, 2);
    return res({ content, headers: { 'content-type': 'application/json' }, statusCode: 200 });
};

const proxy = (req, to) =>
    new Promise((resolve, reject) =>
        proxyRequest(
            {
                url: `${to}${req.url}`,
                method: req.method,
                body: req.content,
            },
            (err, ans) => (err ? reject(err) : resolve(ans))
        )
    );

const toProxy = [...uniqueAssemblies, ...uniquePdbs].map(x => x[0]);
const shouldBeProxied = ({ url }) => toProxy.filter(x => url.endsWith(x))?.length && url.startsWith('/_framework');

module.exports = (ctx, req, res) => {
    if (shouldBeProxied(req)) {
        return proxy(req, iisUrl);
    } else if (req.url.endsWith(bbjson)) {
        return returnWithTweakedBBJson(res);
    }
};
