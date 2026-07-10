const DEFAULT_API_BASE = 'https://localhost:44340';
let apiBase = localStorage.getItem('capstock_api_base') || DEFAULT_API_BASE;
let allItems = [];
let editingId = null;
let pendingFile = null;

const $ = (sel) => document.querySelector(sel);
const $$ = (sel) => document.querySelectorAll(sel);

function apiUrl(path){ return apiBase.replace(/\/$/,'') + '/api/inventory' + path; }
function logsUrl(path){ return apiBase.replace(/\/$/,'') + '/api/logs' + path; }

async function apiFetch(path, options={}){
  const res = await fetch(apiUrl(path), options);
  if(!res.ok){
    let detail = '';
    try{ detail = await res.text(); }catch(e){}
    throw new Error(`${res.status} ${res.statusText}${detail ? ' — ' + detail.slice(0,200) : ''}`);
  }
  const text = await res.text();
  return text ? JSON.parse(text) : null;
}

/* ---------- carregamento ---------- */
async function loadItems(){
  setApiStatus('checking');
  try{
    const data = await apiFetch('');
    allItems = data || [];
    setApiStatus('ok');
    render();
  }catch(err){
    setApiStatus('err', err.message);
    allItems = [];
    render();
  }
}

function setApiStatus(state, detail){
  const dot = $('#api-dot');
  const dotMobile = $('#api-dot-mobile');
  const text = $('#api-status-text');
  const cls = 'dot' + (state==='ok' ? ' ok' : state==='err' ? ' err' : '');
  dot.className = cls;
  if(dotMobile) dotMobile.className = cls;
  if(state==='ok') text.textContent = 'conectado — ' + apiBase.replace(/^https?:\/\//,'');
  else if(state==='err') text.textContent = 'falha ao conectar';
  else text.textContent = 'verificando conexão…';
  text.title = detail || '';
}

/* ---------- filtros ---------- */
let activeCategoria = '';
function currentFilters(){
  return {
    categoria: activeCategoria,
    unidade: $('#filter-unidade').value.trim().toLowerCase(),
    andar: $('#filter-andar').value.trim().toLowerCase(),
    local: $('#filter-local').value.trim().toLowerCase(),
    search: $('#search').value.trim().toLowerCase(),
  };
}

function itemMatches(item, f){
  if(f.categoria && item.categoria !== f.categoria) return false;
  if(f.unidade && !(item.unidade||'').toLowerCase().includes(f.unidade)) return false;
  if(f.andar && !(item.andar||'').toLowerCase().includes(f.andar)) return false;
  if(f.local && !(item.local||'').toLowerCase().includes(f.local)) return false;
  if(f.search){
    const haystack = [
      item.hostname, item.marca, item.modelo, item.patrimonio, item.serialNumber,
      item.ip, item.numeroSerie, item.item, item.local, item.andar, item.unidade
    ].filter(Boolean).join(' ').toLowerCase();
    if(!haystack.includes(f.search)) return false;
  }
  return true;
}

/* ---------- render ---------- */
function render(){
  const f = currentFilters();
  const filtered = allItems.filter(i => itemMatches(i, f));

  // contadores
  $('#count-all').textContent = allItems.length;
  ['Computador','Impressora','ImpressoraTermica','MaterialEstoque'].forEach(cat=>{
    const n = allItems.filter(i=>i.categoria===cat || (cat==='Computador' && i.categoria==='Notebook')).length;
    const el = document.getElementById('count-'+cat);
    if(el) el.textContent = n;
  });
  $('#result-count').textContent = filtered.length + (filtered.length===1 ? ' item' : ' itens');

  const region = $('#table-region');
  if(filtered.length === 0){
    region.innerHTML = `
      <div class="empty">
        <div class="glyph">nada por aqui</div>
        <h3>Nenhum item encontrado</h3>
        <p>Ajuste os filtros, importe uma planilha ou cadastre o primeiro item manualmente.</p>
        <button class="btn btn-signal" onclick="openItemModal()">+ Novo item</button>
      </div>`;
    return;
  }

  const rows = filtered.map(rowHtml).join('');
  region.innerHTML = `
    <table>
      <thead><tr>
        <th>Categoria</th><th>Unidade</th><th>Identificação</th><th>Detalhes</th><th>Local</th><th></th>
      </tr></thead>
      <tbody>${rows}</tbody>
    </table>`;

  $$('.icon-btn.edit').forEach(b=>b.addEventListener('click', ()=>openItemModal(b.dataset.id)));
  $$('.icon-btn.danger').forEach(b=>b.addEventListener('click', ()=>deleteItem(b.dataset.id)));
}

function tagClass(cat){
  const map = {Computador:'tag-computador',Notebook:'tag-notebook',Impressora:'tag-impressora',
    ImpressoraTermica:'tag-impressoratermica',MaterialEstoque:'tag-materialestoque'};
  return map[cat] || 'tag-materialestoque';
}
function catLabel(cat){
  const map = {Computador:'Computador',Notebook:'Notebook',Impressora:'Impressora',
    ImpressoraTermica:'Etiquetadora',MaterialEstoque:'Estoque'};
  return map[cat] || cat;
}

function rowHtml(item){
  const tag = `<span class="tag ${tagClass(item.categoria)}">${catLabel(item.categoria)}</span>`;
  let idCell = '', detailCell = '';

  if(item.categoria==='Computador' || item.categoria==='Notebook'){
    idCell = `<span class="mono">${esc(item.hostname||'—')}</span>`;
    detailCell = `${esc(item.marca||'')} ${esc(item.modelo||'')}<br><span class="muted mono">${esc(item.serialNumber||'sem serial')}</span>`;
  } else if(item.categoria==='Impressora'){
    idCell = `<span class="mono">${esc(item.ip||'—')}</span>`;
    detailCell = `${esc(item.marca||'')} ${esc(item.modelo||'')}<br><span class="muted mono">${esc(item.numeroSerie||'sem serial')}</span>`;
  } else if(item.categoria==='ImpressoraTermica'){
    idCell = `<span class="mono">${esc(item.ip||'—')}</span>`;
    detailCell = `${esc(item.uso||item.modelo||'')}<br><span class="muted mono">${esc(item.numeroSerie||'sem serial')}</span>`;
  } else {
    idCell = `<strong>${esc(item.item||'—')}</strong>`;
    detailCell = `Qtd: ${item.quantidade ?? '—'} · ${esc(item.status||'')} <br><span class="muted">${esc(item.marca||'')}</span>`;
  }

  return `<tr>
    <td class="tag-cell">${tag}</td>
    <td data-label="Unidade"><span class="mono">${esc(item.unidade||'—')}</span></td>
    <td data-label="Identificação">${idCell}</td>
    <td data-label="Detalhes">${detailCell}</td>
    <td data-label="Local">${esc(item.andar||'')} ${item.andar&&item.local?'·':''} ${esc(item.local||'')}</td>
    <td class="actions-cell"><div class="row-actions">
      <button class="icon-btn edit" data-id="${item.id}" title="Editar">✎</button>
      <button class="icon-btn danger" data-id="${item.id}" title="Excluir">🗑</button>
    </div></td>
  </tr>`;
}

function esc(s){ return (s??'').toString().replace(/[&<>"]/g, c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c])); }

/* ---------- gaveta mobile ---------- */
function openDrawer(){ $('#sidebar').classList.add('open'); $('#drawer-backdrop').classList.add('open'); }
function closeDrawer(){ $('#sidebar').classList.remove('open'); $('#drawer-backdrop').classList.remove('open'); }
const btnDrawer = $('#btn-drawer');
if(btnDrawer) btnDrawer.addEventListener('click', openDrawer);
const backdrop = $('#drawer-backdrop');
if(backdrop) backdrop.addEventListener('click', closeDrawer);

/* ---------- sidebar interações ---------- */
$$('.chip-btn').forEach(btn=>{
  btn.addEventListener('click', ()=>{
    $$('.chip-btn').forEach(b=>b.classList.remove('active'));
    btn.classList.add('active');
    activeCategoria = btn.dataset.cat;
    render();
    if(window.innerWidth<=900) closeDrawer();
  });
});
$('#filter-unidade').addEventListener('input', render);
$('#filter-andar').addEventListener('input', render);
$('#filter-local').addEventListener('input', render);
$('#search').addEventListener('input', render);

/* ---------- modal genérico ---------- */
function openOverlay(id){ $('#'+id).classList.add('open'); }
function closeOverlay(id){ $('#'+id).classList.remove('open'); }
$$('[data-close]').forEach(el=>el.addEventListener('click', (e)=>{
  closeOverlay(e.target.closest('.overlay').id);
}));
$$('.overlay').forEach(ov=>ov.addEventListener('click', (e)=>{ if(e.target===ov) closeOverlay(ov.id); }));

/* ---------- criar / editar item ---------- */
const FIELD_GROUPS = ['fields-computador','fields-impressora','fields-impressoratermica','fields-materialestoque'];
function showFieldsFor(categoria){
  FIELD_GROUPS.forEach(g=>document.getElementById(g).style.display='none');
  const map = {Computador:'fields-computador',Notebook:'fields-computador',Impressora:'fields-impressora',
    ImpressoraTermica:'fields-impressoratermica',MaterialEstoque:'fields-materialestoque'};
  const target = map[categoria];
  if(target) document.getElementById(target).style.display='block';
}
$('#f-categoria').addEventListener('change', e=>showFieldsFor(e.target.value));

function clearItemForm(){
  ['f-unidade','f-andar','f-local','c-hostname','c-marca','c-modelo','c-patrimonio','c-serial','c-ssdhd','c-so','c-ram',
   'c-processador','c-mon-sn','c-mon-pat','c-mon-modelo','p-marca','p-modelo','p-ip','p-serial','p-ramal','p-status',
   't-marca','t-modelo','t-ip','t-serial','t-uso','t-status','m-item','m-qtd','m-status','m-marca']
   .forEach(id=>{ const el=document.getElementById(id); if(el) el.value=''; });
  $('#item-msg').className='msg';
}

function openItemModal(id){
  editingId = id || null;
  clearItemForm();
  $('#item-modal-title').textContent = id ? 'Editar item' : 'Novo item';

  if(id){
    const item = allItems.find(i=>i.id===id);
    if(!item) return;
    $('#f-categoria').value = (item.categoria==='Notebook') ? 'Notebook' : item.categoria;
    showFieldsFor(item.categoria);
    $('#f-unidade').value = item.unidade||'';
    $('#f-andar').value = item.andar||'';
    $('#f-local').value = item.local||'';

    if(item.categoria==='Computador'||item.categoria==='Notebook'){
      $('#c-hostname').value=item.hostname||''; $('#c-marca').value=item.marca||'';
      $('#c-modelo').value=item.modelo||''; $('#c-patrimonio').value=item.patrimonio||'';
      $('#c-serial').value=item.serialNumber||''; $('#c-ssdhd').value=item.ssdHd||'';
      $('#c-so').value=item.sistemaOperacional||''; $('#c-ram').value=item.memoriaRam||'';
      $('#c-processador').value=item.processador||'';
      const mon = (item.monitores&&item.monitores[0])||{};
      $('#c-mon-sn').value=mon.numeroSerie||''; $('#c-mon-pat').value=mon.patrimonio||''; $('#c-mon-modelo').value=mon.modelo||'';
    } else if(item.categoria==='Impressora'){
      $('#p-marca').value=item.marca||''; $('#p-modelo').value=item.modelo||'';
      $('#p-ip').value=item.ip||''; $('#p-serial').value=item.numeroSerie||'';
      $('#p-ramal').value=item.ramal||''; $('#p-status').value=item.status||'';
    } else if(item.categoria==='ImpressoraTermica'){
      $('#t-marca').value=item.marca||''; $('#t-modelo').value=item.modelo||'';
      $('#t-ip').value=item.ip||''; $('#t-serial').value=item.numeroSerie||'';
      $('#t-uso').value=item.uso||''; $('#t-status').value=item.status||'';
    } else {
      $('#m-item').value=item.item||''; $('#m-qtd').value=item.quantidade??'';
      $('#m-status').value=item.status||''; $('#m-marca').value=item.marca||'';
    }
  } else {
    $('#f-categoria').value='Computador';
    showFieldsFor('Computador');
  }
  openOverlay('overlay-item');
}
$('#btn-new').addEventListener('click', ()=>openItemModal());

function buildPayload(){
  const categoria = $('#f-categoria').value;
  const base = { categoria, unidade: $('#f-unidade').value||null, andar: $('#f-andar').value||null, local: $('#f-local').value||null };

  if(categoria==='Computador'||categoria==='Notebook'){
    const monitores=[];
    if($('#c-mon-sn').value||$('#c-mon-pat').value||$('#c-mon-modelo').value){
      monitores.push({numeroSerie:$('#c-mon-sn').value||null,patrimonio:$('#c-mon-pat').value||null,modelo:$('#c-mon-modelo').value||null});
    }
    return {...base, hostname:$('#c-hostname').value||null, marca:$('#c-marca').value||null, modelo:$('#c-modelo').value||null,
      patrimonio:$('#c-patrimonio').value||null, serialNumber:$('#c-serial').value||null, ssdHd:$('#c-ssdhd').value||null,
      sistemaOperacional:$('#c-so').value||null, memoriaRam:$('#c-ram').value||null, processador:$('#c-processador').value||null,
      monitores};
  }
  if(categoria==='Impressora'){
    return {...base, marca:$('#p-marca').value||null, modelo:$('#p-modelo').value||null, ip:$('#p-ip').value||null,
      numeroSerie:$('#p-serial').value||null, ramal:$('#p-ramal').value||null, status:$('#p-status').value||null};
  }
  if(categoria==='ImpressoraTermica'){
    return {...base, marca:$('#t-marca').value||null, modelo:$('#t-modelo').value||null, ip:$('#t-ip').value||null,
      numeroSerie:$('#t-serial').value||null, uso:$('#t-uso').value||null, status:$('#t-status').value||null};
  }
  return {...base, item:$('#m-item').value||null, quantidade: $('#m-qtd').value ? Number($('#m-qtd').value) : null,
    status:$('#m-status').value||null, marca:$('#m-marca').value||null};
}

$('#btn-save-item').addEventListener('click', async ()=>{
  const msg = $('#item-msg');
  msg.className='msg'; msg.textContent='';
  const payload = buildPayload();
  const btn = $('#btn-save-item');
  btn.disabled = true; btn.textContent='Salvando…';
  try{
    if(editingId){
      await apiFetch('/'+editingId, {method:'PUT', headers:{'Content-Type':'application/json'}, body:JSON.stringify(payload)});
    } else {
      await apiFetch('', {method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify(payload)});
    }
    closeOverlay('overlay-item');
    await loadItems();
  }catch(err){
    msg.className='msg err show';
    msg.textContent = 'Não foi possível salvar: ' + err.message;
  }finally{
    btn.disabled=false; btn.textContent='Salvar item';
  }
});

async function deleteItem(id){
  if(!confirm('Excluir este item do inventário? Essa ação não pode ser desfeita.')) return;
  try{
    await apiFetch('/'+id, {method:'DELETE'});
    await loadItems();
  }catch(err){
    alert('Não foi possível excluir: ' + err.message);
  }
}

/* ---------- exportar ---------- */
function doExport(formato){
  const f = currentFilters();
  const params = new URLSearchParams();
  if(f.categoria) params.set('categoria', f.categoria);
  if(f.unidade) params.set('unidade', f.unidade);
  if(f.andar) params.set('andar', f.andar);
  if(f.local) params.set('local', f.local);
  params.set('formato', formato);
  window.open(apiUrl('/export') + '?' + params.toString(), '_blank');
}
$('#btn-export-xlsx').addEventListener('click', ()=>doExport('xlsx'));
$('#btn-export-csv').addEventListener('click', ()=>doExport('csv'));

/* ---------- importar planilha ---------- */
function detectUnidadeFromFileName(fileName){
  let name = fileName.replace(/\.(xlsx|csv)$/i, '').trim();
  const lower = name.toLowerCase();
  const prefixos = ['unidade ', 'unidade_', 'unidade-', 'unidade'];
  for(const p of prefixos){
    if(lower.startsWith(p)){
      const resto = name.slice(p.length).replace(/^[\s_-]+/, '').trim();
      return resto || name;
    }
  }
  return name;
}

$('#btn-import').addEventListener('click', ()=>{
  pendingFile = null;
  $('#import-summary').innerHTML='';
  $('#import-msg').className='msg';
  $('#btn-do-import').disabled = true;
  $('#dropzone-label').innerHTML = '<strong>Clique para escolher</strong> ou arraste o arquivo .xlsx ou .csv aqui';
  $('#csv-categoria-field').style.display = 'none';
  $('#csv-categoria').value = '';
  $('#unidade-field').style.display = 'none';
  $('#import-unidade').value = '';
  openOverlay('overlay-import');
});
const dropzone = $('#dropzone');
dropzone.addEventListener('click', ()=>$('#file-input').click());
dropzone.addEventListener('dragover', e=>{e.preventDefault(); dropzone.classList.add('drag');});
dropzone.addEventListener('dragleave', ()=>dropzone.classList.remove('drag'));
dropzone.addEventListener('drop', e=>{
  e.preventDefault(); dropzone.classList.remove('drag');
  if(e.dataTransfer.files[0]) selectFile(e.dataTransfer.files[0]);
});
$('#file-input').addEventListener('change', e=>{ if(e.target.files[0]) selectFile(e.target.files[0]); });

function selectFile(file){
  const name = file.name.toLowerCase();
  const isXlsx = name.endsWith('.xlsx');
  const isCsv = name.endsWith('.csv');
  if(!isXlsx && !isCsv){
    $('#import-msg').className='msg err show';
    $('#import-msg').textContent='Apenas arquivos .xlsx ou .csv são aceitos.';
    return;
  }
  pendingFile = file;
  $('#import-msg').className='msg';
  $('#csv-categoria-field').style.display = isCsv ? 'block' : 'none';
  $('#unidade-field').style.display = 'block';
  $('#import-unidade').value = detectUnidadeFromFileName(file.name);
  $('#dropzone-label').innerHTML = `<strong>${esc(file.name)}</strong> selecionado — clique em "Importar arquivo"`;
  $('#btn-do-import').disabled = false;
}

$('#btn-do-import').addEventListener('click', async ()=>{
  if(!pendingFile) return;
  const btn = $('#btn-do-import');
  btn.disabled = true; btn.textContent = 'Importando…';
  $('#import-msg').className='msg';
  $('#import-summary').innerHTML='';
  try{
    const form = new FormData();
    form.append('file', pendingFile);

    const isCsv = pendingFile.name.toLowerCase().endsWith('.csv');
    const categoriaCsv = $('#csv-categoria').value;
    const unidade = $('#import-unidade').value.trim();

    const params = new URLSearchParams();
    if(isCsv && categoriaCsv) params.set('categoria', categoriaCsv);
    if(unidade) params.set('unidade', unidade);
    const query = params.toString() ? '?' + params.toString() : '';

    const res = await fetch(apiUrl('/import') + query, {method:'POST', body: form});
    if(!res.ok){
      const detail = await res.text();
      throw new Error(`${res.status} ${res.statusText} — ${detail.slice(0,200)}`);
    }
    const result = await res.json();
    $('#import-msg').className='msg ok show';
    $('#import-msg').textContent = `Unidade "${result.unidade||'—'}": ${result.totalItensImportados} item(ns) importado(s)`
      + (result.itensSubstituidos ? `, substituindo ${result.itensSubstituidos} item(ns) que já existiam dessa unidade.` : '.');
    $('#import-summary').innerHTML = result.planilhas.map(p=>`
      <div class="import-row">
        <div style="width:100%;">
          <div class="name">${esc(p.nomePlanilha)}</div>
          <div class="muted">categoria: ${esc(p.categoriaPadrao)} · ${p.itensImportados} importado(s)${p.linhasIgnoradas?', '+p.linhasIgnoradas+' ignorada(s)':''}</div>
          ${p.avisos && p.avisos.length ? '<ul>'+p.avisos.map(a=>'<li class="warn">'+esc(a)+'</li>').join('')+'</ul>' : ''}
          ${renderColunasDetectadas(p.colunasDetectadas)}
        </div>
      </div>`).join('');
    await loadItems();
  }catch(err){
    $('#import-msg').className='msg err show';
    $('#import-msg').textContent = 'Falha na importação: ' + err.message;
  }finally{
    btn.disabled = false; btn.textContent = 'Importar arquivo';
  }
});

function renderColunasDetectadas(cols){
  if(!cols) return '';
  const entries = Object.entries(cols);
  if(entries.length === 0) return '';
  const chips = entries.map(([campo, ok])=>
    `<span class="col-chip ${ok?'ok':'miss'}" title="${ok?'coluna encontrada no arquivo':'nenhuma coluna com esse nome foi encontrada no arquivo'}">${ok?'✓':'✗'} ${esc(campo)}</span>`
  ).join('');
  const faltando = entries.filter(([,ok])=>!ok).length;
  return `<div class="col-diagnostico">${chips}</div>` +
    (faltando > 0
      ? `<p class="muted" style="font-size:11px;margin:6px 0 0;">${faltando} coluna(s) não encontrada(s) no cabeçalho do arquivo — os campos correspondentes ficaram em branco. Renomeie a coluna no arquivo pra um dos nomes esperados e reimporte.</p>`
      : '');
}

/* ---------- histórico de atividade ---------- */
$('#btn-logs').addEventListener('click', ()=>{
  $('#logs-filter-unidade').value = $('#filter-unidade').value || '';
  openOverlay('overlay-logs');
  loadLogs();
});
$('#btn-logs-refresh').addEventListener('click', loadLogs);
$('#logs-filter-unidade').addEventListener('keydown', e=>{ if(e.key==='Enter') loadLogs(); });
$('#logs-filter-acao').addEventListener('change', loadLogs);

async function loadLogs(){
  const list = $('#logs-list');
  list.innerHTML = '<p class="muted" style="font-size:12.5px;">Carregando…</p>';
  try{
    const params = new URLSearchParams();
    const unidade = $('#logs-filter-unidade').value.trim();
    const acao = $('#logs-filter-acao').value;
    if(unidade) params.set('unidade', unidade);
    if(acao) params.set('acao', acao);
    params.set('limit', '150');

    const res = await fetch(logsUrl('?' + params.toString()));
    if(!res.ok) throw new Error(`${res.status} ${res.statusText}`);
    const logs = await res.json();
    if(!logs || logs.length === 0){
      list.innerHTML = '<p class="muted" style="font-size:12.5px;">Nenhuma atividade registrada ainda com esses filtros.</p>';
      return;
    }
    list.innerHTML = logs.map(l=>`
      <div class="log-row">
        <div>
          <span class="log-acao ${esc(l.acao)}">${esc(l.acao)}</span>
          <span class="desc">${esc(l.descricao)}</span>
          <div class="meta">${l.unidade?esc(l.unidade)+' · ':''}${l.categoria?esc(l.categoria):''}</div>
        </div>
        <div class="when">${formatTimestamp(l.timestamp)}</div>
      </div>`).join('');
  }catch(err){
    list.innerHTML = `<p style="color:var(--red);font-size:12.5px;">Falha ao carregar histórico: ${esc(err.message)}</p>`;
  }
}

function formatTimestamp(iso){
  try{
    const d = new Date(iso);
    return d.toLocaleString('pt-BR', {day:'2-digit',month:'2-digit',hour:'2-digit',minute:'2-digit'});
  }catch(e){ return iso; }
}

/* ---------- config da api ---------- */
$('#btn-config').addEventListener('click', ()=>{
  $('#api-base-input').value = apiBase;
  openOverlay('overlay-config');
});
$('#btn-save-config').addEventListener('click', ()=>{
  const val = $('#api-base-input').value.trim();
  if(val){
    apiBase = val.replace(/\/$/,'');
    localStorage.setItem('capstock_api_base', apiBase);
  }
  closeOverlay('overlay-config');
  loadItems();
});

/* ---------- init ---------- */
loadItems();
