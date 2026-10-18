#!/usr/bin/env python3
from pathlib import Path
import argparse,shutil,subprocess,tempfile
p=argparse.ArgumentParser();p.add_argument('--mono',default=shutil.which('mono'));p.add_argument('--csc',default=shutil.which('mcs'));a=p.parse_args()
if not a.mono or not a.csc:p.error('Mono and C# compiler are required')
root=Path(__file__).resolve().parents[2];src=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
for file in ['PlayerTaskTrack.cs','ReferenceWorldPlayerTaskTrack.cs']:
 text=(src/file).read_text();assert 'layout.Update(lines' in text and 'layout.Rows.Count' in text and 'layout.Widths[' in text
 assert 'Mathf.Min' in text and 'margin' in text
assert (src/'TaskClient.State.cs').read_text().index('ObserveTaskCompletionReceipt') < (src/'TaskClient.State.cs').read_text().index('OnStateChanged?.Invoke')
with tempfile.TemporaryDirectory(prefix='quest-feedback-') as temp:
 exe=Path(temp)/'Contract.exe';command=[a.mono,a.csc] if a.csc.endswith('.exe') else [a.csc]
 subprocess.run(command+['-nologo','-out:'+str(exe)]+[str(src/name) for name in ['DialogUI.TaskCompletion.cs','TaskPromptLayout.cs','TaskClient.State.cs','TaskClient.Offers.cs','ReferenceNpcNoticeReceipt.cs']]+[str(Path(__file__).with_name('QuestFeedbackContract.cs'))],check=True)
 subprocess.run([a.mono,str(exe)],check=True)
print('PASS: both live and snapshot prompt renderers use measured wrapping; receipt closes before observers')
