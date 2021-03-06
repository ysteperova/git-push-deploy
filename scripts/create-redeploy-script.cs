//@auth
//@req(url, next, targetEnv, nodeGroup)

import com.hivext.api.core.utils.Transport;
import com.hivext.api.utils.Random;

//reading script from URL
var scriptBody = new Transport().get(url)

//inject token
var token = Random.getPswd(64);
scriptBody = scriptBody.replace("${TOKEN}", token);

targetEnv = targetEnv.toString().split(".")[0];
scriptBody = scriptBody.replace("${TARGET_ENV}", targetEnv);
scriptBody = scriptBody.replace("${NODE_GROUP}", nodeGroup.toString());
var buildEnv = "${env.envName}";
scriptBody = scriptBody.replace("${BUILD_ENV}", buildEnv);
scriptBody = scriptBody.replace("${BUILD_ENV_APPID}", "${env.appid}");
scriptBody = scriptBody.replace("${BUILD_NODE_ID}", "${nodes.build.first.id}");
scriptBody = scriptBody.replace("${UID}", user.uid.toString());

//getting master node     
var certified = true;
var resp = jelastic.env.control.GetEnvInfo(targetEnv, session);
if (resp.result != 0) return resp;
var nodes = resp.nodes;
var build = false;
for (var i = 0; i < nodes.length; i++) {
   if (nodes[i].nodeGroup == nodeGroup) {
       type = nodes[i].engineType || (nodes[i].activeEngine || {}).type
       certified = type ? true : false;
       if (type == "java") build = true;
       break;
   }
}

scriptBody = scriptBody.replace("${CERTIFIED}", certified.toString());
scriptBody = scriptBody.replace("${BUILD}", build.toString());

if (build) {
   var projectId = parseInt("${nodes.build.first.customitem.projects[0].id}", 10);
   var projectName = "${nodes.build.first.customitem.projects[0].name}";
   if (isNaN(projectId)) {
      var project = jelastic.env.control.GetEnvInfo(buildEnv, session).nodes[0].customitem.projects[0];
      projectId = project.id;
      projectName = project.name;
   }

   scriptBody = scriptBody.replace("${PROJECT_NAME}", projectName.toString());
   scriptBody = scriptBody.replace("${PROJECT_ID}", projectId.toString());
}

var scriptName = "${env.envName}-${globals.scriptName}"; 
//delete the script if it exists already
jelastic.dev.scripting.DeleteScript(scriptName);

//create a new script 
var resp = jelastic.dev.scripting.CreateScript(scriptName, 'js', scriptBody);
if (resp.result != 0) return resp;

//get app host
var version = jelastic.system.service.GetVersion().version.split("-").shift();
var host;
if (version < '4.10'){
   var domain = jelastic.dev.apps.GetApp(appid).hosting.domain;
   host = 'http://' + domain;
} else {
   host = 'https://' + window.location.host.replace('app.', 'cs.');
}
if (window.location.host) 
return {
    result: 0,
    onAfterReturn : {
        call : {
            procedure : next,
            params : {
                appid : appid,
                host : host,
                token : token
            }
        }
    }
}
