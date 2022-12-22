import { Component, OnInit, Input, ViewEncapsulation, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-operation-type-dropdown',
  templateUrl: './operation-type-dropdown.component.html',
  styleUrls: ['./operation-type-dropdown.component.scss'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})

export class OperationTypeDropdownComponent implements OnInit {
  @Output() selectedOption = new EventEmitter<string>();
  @Input() isDisabled: boolean;
  textFeatureShow = true;

  @Input() value;

  constructor() { }

  ngOnInit() {
  }

  showSelectedValue(data: string) {
    this.selectedOption.emit(data);
  }
}
