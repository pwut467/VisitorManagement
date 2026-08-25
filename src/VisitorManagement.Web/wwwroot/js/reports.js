$(function () {
    const table = $('#report-table');
    if (!table.length) {
        return;
    }

    table.DataTable({
        language: {
            decimal: '',
            emptyTable: 'ไม่มีข้อมูลในตาราง',
            info: 'แสดง _START_ ถึง _END_ จาก _TOTAL_ รายการ',
            infoEmpty: 'แสดง 0 ถึง 0 จาก 0 รายการ',
            infoFiltered: '(กรองจากทั้งหมด _MAX_ รายการ)',
            lengthMenu: 'แสดง _MENU_ รายการ',
            loadingRecords: 'กำลังโหลด...',
            processing: 'กำลังประมวลผล...',
            search: 'ค้นหา:',
            zeroRecords: 'ไม่พบรายการที่ค้นหา',
            paginate: {
                first: 'หน้าแรก',
                last: 'หน้าสุดท้าย',
                next: 'ถัดไป',
                previous: 'ก่อนหน้า'
            }
        },
        order: [],
        pageLength: 25,
        lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, 'ทั้งหมด']],
        columnDefs: [
            { targets: 0, orderable: false, searchable: false, width: '4.5rem', className: 'col-seq text-center' }
        ],
        drawCallback: function () {
            const api = this.api();
            const start = api.page.info().start;
            api.column(0, { page: 'current', order: 'applied', search: 'applied' })
                .nodes()
                .each(function (cell, i) {
                    cell.textContent = start + i + 1;
                });
        }
    });
});
