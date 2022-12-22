import { Component, OnInit, Output, EventEmitter, Input, ViewEncapsulation, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-column-dropdown',
  templateUrl: './column-dropdown.component.html',
  styleUrls: ['./column-dropdown.component.scss'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ColumnDropdownComponent implements OnInit {
  @Output() selectedColumnOption = new EventEmitter<string>();
  @Input() isDisabled: boolean;
  @Input() options: Array<string>;
  @Input() optionsListForValuePopUp: Array<string>;
  @Input() defaultOptionName: string;
  @Input() value;
  constructor() { }

  ngOnInit() {
  }

  // On Change Emit value to the calling component
  sendSelectedColumn(data: string) {
    this.selectedColumnOption.emit(data);
  }

  trackByOption(index, item) {
    if (!item) { return null; }
    return index;
  }

  trackByOptionValue(index, item) {
    if (!item) { return null; }
    return index;
  }
}
