import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { OperationTypeDropdownComponent } from './operation-type-dropdown.component';

describe('OperationTypeDropdownComponent', () => {
  let component: OperationTypeDropdownComponent;
  let fixture: ComponentFixture<OperationTypeDropdownComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ OperationTypeDropdownComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(OperationTypeDropdownComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
