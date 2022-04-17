// TODO: поддержка соединений

document.addEventListener("DOMContentLoaded", main);
const Application = {
	actionList: [],
	actionListSort: {},
	defauleMarkIcon: 'icon-mark-1',
	defaultWayfarerIcon: 'icon-mark-1',
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
		offset: { x: -12, y: -24 },
		data: `<g transform="translate(-12, -24)" class="icon-mark-1"><polygon class="a" points="20 7.75 4 7.75 2 3 22 3 20 7.75"/><polyline class="a" points="2 3 22 3 20 7.75"/><polygon class="a" points="17 14.875 7 14.875 5 10.125 19 10.125 17 14.875"/><polygon class="a" points="14 22 10 22 8 17.25 16 17.25 14 22"/></g>`
	}
}
