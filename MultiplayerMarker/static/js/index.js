// TODO: поддержка соединений

document.addEventListener("DOMContentLoaded", main);
const Application = {
	actionList: [],
	actionListSort: {},
	defauleMarkIcon: 'icon-cactus-1',
	defaultWayfarerIcon: 'icon-wayfarer-1',
	hubConnection: null,
	svg: null,
	users: [],
	currentUser: null,
	currentId: null,
	icons: {},
	constants: {
		ActionType: {
			MarkAdded: 0,
			MarkRemoved: 1,
			ClearMarks: 2
		},
		ActionTypeNames: ['MarkAdded', 'MarkRemoved', 'ClearMarks']
    }
};

function showMessage(message) {
    alert(message);
};

async function configureGameHub() {
	const hubConnection = new signalR.HubConnectionBuilder()
		.withUrl("/gamehub")
		.build();

	Application.hubConnection = hubConnection;

	hubConnection.on('UserAdded', handleUserAdded);
	hubConnection.on('MarkAdded', handleMarkAdded);
	hubConnection.on('MarkRemoved', handleMarkRemoved);
	hubConnection.on('StartMove', handleStartMove);
	hubConnection.on('StopMove', handleStopMove);
	hubConnection.on('MarksCleeared', handleMarksCleared);

	return hubConnection.start().then(_ => Application.currentId = hubConnection.connectionId);
};

/* recive */
function handleUserAdded(user) {
	if (user.userId === Application.currentId) {
		Application.currentUser = user;
		currentUserAdded();
	}

	const { users, defauleMarkIcon, defaultWayfarerIcon } = Application;

	user.markIcon = user.markIcon || defauleMarkIcon;
	user.wayfarerIcon = user.wayfarerIcon || defaultWayfarerIcon;

	users.push(user);
	drowUser(user);

	const marks = user.marks || [];
	user.marks = [];

	marks.forEach(m => handleMarkAdded(m, user.userId)); // Каждый раз идёт поиск пользователя. Переделать
}

function handleMarkAdded(mark, userId) {
	const user = Application.users.find(u => u.userId === userId);
	if (!user) {
		// TODO: побровать загрузить с сервера или синхронуть
		showMessage('Ошибка. Пользователь не найден');
		return;
	}

	user.marks.push(mark);
	drowMark(mark, user);
}

function handleMarkRemoved(markId, userId) {
	const user = Application.users.find(u => u.userId === userId);
	if (!user) {
		// TODO: побровать загрузить с сервера или синхронуть
		showMessage('Ошибка. Пользователь не найден');
		return;
	}

	const index = user.marks.findIndex(x => x.id === markId);
	if (index >= 0) {
		user.marks.splice(index, 1);
		removeMark(userId, markId);
    }
}

function handleStartMove(userId, wayfarer) {
	drowMove(userId, wayfarer);
}

function handleStopMove(userId) {
	drowStopMove(userId);
}

function handleMarksCleared(userId) {
	clearMarks(userId);
}

function sendSyncronize(users) {
	Application.syncronizing = true;
	// TODO: копить вызовы пока идёт синхронизация.
	Application.hubConnection.invoke('Syncronize')
		.then(users => {
			Application.users = [];
			Application.svg.selectChildren().remove();

			users.forEach(handleUserAdded);
			Application.syncronizing = false;
		});
}

/* send */

function sendAddMark(mark) {
	const { hubConnection } = Application;
	hubConnection.invoke('AddMark', mark);
}

function sendRemoveMark(markId) {
	const { hubConnection } = Application;
	hubConnection.invoke('RemoveMark', markId);
}


/* UI */
function onStartClick() {
	const name = d3.select('#name').node().value;
	if (!name) {
		showMessage('Введите имя');
		return;
	}

	const hubConnection = Application.hubConnection;

	hubConnection.invoke('TryAddUser', name)
		.then(r => {
			if (r.success === false) {
				showMessage(r.message || 'Ошибка');
				return;
			}
		});
}

function onCearClick() {
	Application.hubConnection.invoke('ClearMarks');
}

function currentUserAdded() {
	d3.selectAll('#send-name-button, #name')
		.property('disabled', true);

	d3.selectAll('#clear-marks-button, #import-button, #export-button, #file-input')
		.property('disabled', false);
}

function onMarkDeselect(e) {
	e.stopPropagation();

	const userId = d3.select(this.parentElement).attr('user-id');
	const markId = Number.parseInt(d3.select(this).attr('mark-id'));

	if (userId !== Application.currentId || isNaN(markId)) {
		return; // Лишняя проверка
	}

	sendRemoveMark(markId)
};

function drowMark(mark, user) {
	const icon = Application.icons[user.markIcon]; //TODO: проверка на существование
	const svgMark = Application.svg
		.select(`g.user[user-id="${user.userId}"]`)
		.append('g')
		.attr('class', 'mark')
		.attr('mark-id', mark.id)
		.attr('transform', `translate(${mark.x}, ${mark.y})`)
		.html(icon.data);

	if (user === Application.currentUser) {
		svgMark.on('click', onMarkDeselect);
    }
};

function drowMove(userId, wayfarer) {
	Application.svg
		.select(`g.user[user-id="${userId}"] g.wayfarer`)
		.attr('transform', `translate(${wayfarer.startMark.x}, ${wayfarer.startMark.y})`)
		.attr('visible', true)
		.transition()
		.duration(wayfarer.movementTime) //TODO: поправка на пинг
		.attr('transform', `translate(${wayfarer.endMark.x}, ${wayfarer.endMark.y})`);
}

function drowStopMove(userId) {
	Application.svg
		.select(`g.user[user-id="${userId}"] g.wayfarer`)
		.attr('visible', false);
}

function clearMarks(userId) {
	Application.svg
		.selectAll(`g.user[user-id="${userId}"] g.mark`)
		.remove();
}

function drowUser(user) {
	const icon = Application.icons[user.wayfarerIcon];
	Application.svg.append('g')
		.attr('class', `user${user === Application.currentUser ? ' current-user' : ''}`)
		.attr('user-id', user.userId)
		.append('g')
		.attr('class', 'wayfarer')
		.attr('visible', false)
		.html(icon.data);
};

function removeMark(userId, markId) {
	Application.svg
		.select(`g.user[user-id="${userId}"] g.mark[mark-id="${markId}"]`)
		.remove();
};

function onMarkAdded(e) {
	if (!Application.currentUser) {
		showMessage("Создайте пользователя");
		return;
	}

	const [x, y] = d3.pointer(e);
	
	const newMark = {
		x: x,
		y: y
	};

	sendAddMark(newMark);
}

function sortActionList() {
	const { actionListSort: sort} = Application;
	const colType = d3.select(this).attr('coltype');
	if (colType === 'index') {
		return;
    }
	if (sort.colType === colType) {
		sort.condition *= -1;
	} else {
		sort.colType = colType;
		sort.condition = 1;
	}

	drowActionList();
}

function drowActionList() {
	const { actionListSort: sort, actionList: list, constants: cons } = Application;
	d3.select('div.action-list table thead th[sortby]').attr('sortby', null);
	d3.select(`div.action-list table thead th[coltype="${sort.colType}"]`).attr('sortby', sort.condition);
	// TODO: переделать на .data()

	d3.selectAll('div.action-list table tbody tr').remove();

	const tbody = d3.select('div.action-list table tbody');
	
	list.sort((d1, d2) => {
		d1 = d1[sort.colType];
		d2 = d2[sort.colType]
		if(d1 > d2){
			return sort.condition;
		}
		if (d2 > d1) {
			return -sort.condition;
		}

		return 0;
	})
		.forEach((d, i) => {
			const row = tbody.append('tr');
			row.append('td').attr('coltype', 'index').text(i + 1);
			row.append('td').attr('coltype', 'DateTime').text((new Date(d.DateTime)).toLocaleString('ru-RU'));
			row.append('td').attr('coltype', 'UserName').text(d.UserName);
			row.append('td').attr('coltype', 'ActionType')
				.attr('colspan', d.ActionType === cons.ActionType.ClearMarks ? 3 : 1)
				.text(cons.ActionTypeNames[d.ActionType]);

			if (d.ActionType !== cons.ActionType.ClearMarks) {
				row.append('td').attr('coltype', 'X').text(d.X);
				row.append('td').attr('coltype', 'Y').text(d.Y);
			}
		});
}

function refreshActionList() {
	d3.json('/Home/ActionList').then(list => {
		Application.actionList = list;
		drowActionList();

		Application.refreshActionListTimer = setTimeout(refreshActionList, 1000 * 5);
	});
}

/* Utility */

function exportFn() {
	saveFile(JSON.stringify(Application.currentUser.marks));
}

function importFn() {
	const file = d3.select('#file-input').node().files[0];
	if (!file) {
		return;
	}

	const reader = new FileReader();
	reader.onload = function (e) {
		const result = JSON.parse(e.target.result);
		// TODO: валидация
		result.forEach(m => {
			sendAddMark({
				x: m.x,
				y: m.y
			});
		});
	};
	reader.readAsText(file);
}

function saveFile(dataText) {
	const blob = new Blob([dataText], { type: "text/plain" });
	const link = document.createElement("a");
	link.setAttribute("href", URL.createObjectURL(blob));
	link.setAttribute("download", "export.json");
	link.click();
}

async function main() {
	Application.svg = d3.select('svg');
	await configureGameHub()
		.then(sendSyncronize);

	d3.select('#send-name-button').on('click', onStartClick);
	d3.select('#clear-marks-button').on('click', onCearClick)
	d3.select('#export-button').on('click', exportFn);
	d3.select('#import-button').on('click', importFn);

	const svg = Application.svg;
	svg.on('click', onMarkAdded);

	Application.actionList = window.AppActionList || []
	d3.selectAll('div.action-list table thead th').on('click', sortActionList);

	Application.refreshActionListTimer = setTimeout(refreshActionList, 1000 * 5);

	Application.icons['icon-mark-1'] = {
		data: `<g transform="translate(-12, -24)" class="icon-mark-1"><polygon class="a" points="20 7.75 4 7.75 2 3 22 3 20 7.75"/><polyline class="a" points="2 3 22 3 20 7.75"/><polygon class="a" points="17 14.875 7 14.875 5 10.125 19 10.125 17 14.875"/><polygon class="a" points="14 22 10 22 8 17.25 16 17.25 14 22"/></g>`
	}

	Application.icons['icon-cactus-1'] = {
		data: `<g transform="translate(-16, -32) scale(0.5, 0.5)" class="icon-cactus-1"><g data-name="Layer 6" id="Layer_6"><g data-name="Layer 18" id="Layer_18"><path class="cls-1" d="M28.8,7.75h.41a7.67,7.67,0,0,1,7.67,7.67V41.75a0,0,0,0,1,0,0H21.13a0,0,0,0,1,0,0V15.42A7.67,7.67,0,0,1,28.8,7.75Z"/><polygon class="cls-2" points="40.31 56.5 15.7 56.5 12.14 46.94 43.86 46.94 40.31 56.5"/><rect class="cls-3" height="5.38" rx="1.9" ry="1.9" width="38.25" x="8.88" y="41.63"/><line class="cls-4" x1="21.69" x2="17.56" y1="16" y2="12.25"/><line class="cls-4" x1="33.87" x2="38.38" y1="14.93" y2="10.82"/><line class="cls-4" x1="22.86" x2="19.5" y1="27.5" y2="24.92"/><line class="cls-4" x1="21.69" x2="17.56" y1="38" y2="34.25"/><line class="cls-4" x1="36.37" x2="40.5" y1="26.72" y2="22.4"/><circle class="cls-5" cx="27.75" cy="28.5" r="1.75"/><circle class="cls-5" cx="25.75" cy="22.5" r="1.75"/><circle class="cls-5" cx="26.75" cy="12.5" r="1.75"/><circle class="cls-5" cx="32.75" cy="32.5" r="1.75"/><circle class="cls-5" cx="25.75" cy="36.5" r="1.75"/><path class="cls-1" d="M48.42,21h-1A3.37,3.37,0,0,0,44,24.58V30H37v7H48.68A3.34,3.34,0,0,0,52,33.52V24.58A3.46,3.46,0,0,0,48.42,21Z"/><circle class="cls-5" cx="48.32" cy="24.9" r="1.15"/><circle class="cls-5" cx="31.75" cy="18.5" r="1.75"/><circle class="cls-5" cx="47.96" cy="33.97" r="1.15"/><line class="cls-4" x1="51.08" x2="55.12" y1="28.23" y2="26.26"/><line class="cls-4" x1="46.19" x2="42.83" y1="22.75" y2="20.17"/><circle class="cls-5" cx="42.96" cy="32.97" r="1.15"/></g></g></g>`
	}

	Application.icons['icon-wayfarer-1'] = {
		data: `<g transform="translate(-16, -32) scale(0.5, 0.5)" class="icon-cactus-1"><g transform="matrix(1,0,0,1,-352,-162.338)"><g id="yacht" transform="matrix(1,0,0,1,18.8303,162.338)"><rect height="64" style="fill:none;" width="64" x="333.17" y="0"/><g transform="matrix(1,0,0,1,269.497,-256)"><path d="M78,278C78,276.895 78.895,276 80,276C84.716,276 96.404,276 100,276C100.669,276 101.293,276.334 101.664,276.891C102.778,278.562 105.07,282 105.07,282L120,282C120.727,282 121.397,282.395 121.749,283.03C122.102,283.666 122.081,284.443 121.696,285.06C118.804,289.688 113.466,298.228 111.696,301.06C111.331,301.645 110.69,302 110,302C104.544,302 80.464,302 72,302C71.336,302 70.716,301.671 70.344,301.121C69.972,300.572 69.897,299.873 70.143,299.257C71.353,296.233 73.247,291.499 74.143,289.257C74.447,288.498 75.182,288 76,288C77.131,288 78.558,288 78.558,288L81.225,280L80,280C78.895,280 78,279.105 78,278L78,278ZM104.472,286L92.472,292L77.354,292L74.954,298L108.892,298L116.392,286L104.472,286ZM101.079,283.224L98.93,280L85.442,280L82.775,288L91.528,288L101.079,283.224Z"/></g></g></g><g>`
	}
}
