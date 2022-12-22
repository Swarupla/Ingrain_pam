import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DataModificationComponent } from './data-modification.component';

describe('DataModificationComponent', () => {
  let component: DataModificationComponent;
  let fixture: ComponentFixture<DataModificationComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ DataModificationComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DataModificationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
