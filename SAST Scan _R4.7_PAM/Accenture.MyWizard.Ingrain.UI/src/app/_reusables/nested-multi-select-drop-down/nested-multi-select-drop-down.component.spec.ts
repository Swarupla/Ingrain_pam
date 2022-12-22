import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { NestedMultiSelectDropDownComponent } from './nested-multi-select-drop-down.component';

describe('NestedMultiSelectDropDownComponent', () => {
  let component: NestedMultiSelectDropDownComponent;
  let fixture: ComponentFixture<NestedMultiSelectDropDownComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ NestedMultiSelectDropDownComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NestedMultiSelectDropDownComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
